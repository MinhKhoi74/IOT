import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import 'models/app_models.dart';
import 'services/api_service.dart';

void main() => runApp(const SmartParkingUserApp());

final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'VND');
final dateFormat = DateFormat('dd/MM/yyyy HH:mm');
final shortDateFormat = DateFormat('dd/MM/yyyy');

const appBg = Color(0xFFF4F7F5);
const ink = Color(0xFF12211D);
const pine = Color(0xFF0F5F4F);
const mint = Color(0xFFE6F3ED);
const amber = Color(0xFFE7A72F);
const softLine = Color(0xFFDCE7E1);

class SmartParkingUserApp extends StatefulWidget {
  const SmartParkingUserApp({super.key});

  @override
  State<SmartParkingUserApp> createState() => _SmartParkingUserAppState();
}

class _SmartParkingUserAppState extends State<SmartParkingUserApp> {
  static const _deeplinkChannel = MethodChannel('smartparking/deeplink');

  final api = ApiService();
  var ready = false;
  var homeIndex = 0;

  @override
  void initState() {
    super.initState();
    _deeplinkChannel.setMethodCallHandler(_handleDeepLinkCall);
    _handleWebReturnUrl();
    api.loadSession().whenComplete(() => setState(() => ready = true));
    _loadInitialDeepLink();
  }

  @override
  Widget build(BuildContext context) {
    final scheme = ColorScheme.fromSeed(
      seedColor: pine,
      brightness: Brightness.light,
      primary: pine,
      secondary: amber,
      surface: Colors.white,
    );

    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'SmartParking',
      theme: ThemeData(
        colorScheme: scheme,
        scaffoldBackgroundColor: appBg,
        useMaterial3: true,
        appBarTheme: const AppBarTheme(
          centerTitle: false,
          backgroundColor: appBg,
          foregroundColor: ink,
          elevation: 0,
          titleTextStyle: TextStyle(
            color: ink,
            fontSize: 20,
            fontWeight: FontWeight.w800,
          ),
        ),
        cardTheme: CardThemeData(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
            side: const BorderSide(color: softLine),
          ),
          margin: EdgeInsets.zero,
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: pine,
            foregroundColor: Colors.white,
            minimumSize: const Size.fromHeight(48),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: pine,
            minimumSize: const Size.fromHeight(48),
            side: const BorderSide(color: pine),
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: softLine),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: pine, width: 1.4),
          ),
        ),
        navigationBarTheme: const NavigationBarThemeData(
          backgroundColor: Colors.white,
          indicatorColor: mint,
          labelTextStyle: WidgetStatePropertyAll(
            TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
          ),
        ),
      ),
      home: !ready
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : api.isSignedIn
              ? HomeShell(
                  api: api, initialIndex: homeIndex, onSignedOut: _signedOut)
              : AuthPage(api: api, onSignedIn: _signedIn),
    );
  }

  void _signedIn() => setState(() {});
  void _signedOut() => setState(() {});

  Future<void> _loadInitialDeepLink() async {
    try {
      final link =
          await _deeplinkChannel.invokeMethod<String>('getInitialLink');
      _handleDeepLink(link);
    } on MissingPluginException {
      // Deep links are currently wired for Android.
    }
  }

  Future<void> _handleDeepLinkCall(MethodCall call) async {
    if (call.method == 'onLink') {
      _handleDeepLink(call.arguments?.toString());
    }
  }

  void _handleDeepLink(String? link) {
    if (link == null || link.isEmpty) return;
    final uri = Uri.tryParse(link);
    if (uri?.scheme == 'smartparking' && uri?.host == 'vehicles') {
      setState(() => homeIndex = 3);
    }
  }

  void _handleWebReturnUrl() {
    if (Uri.base.queryParameters['tab'] == 'vehicles') {
      homeIndex = 3;
    }
  }
}

class AuthPage extends StatefulWidget {
  const AuthPage({required this.api, required this.onSignedIn, super.key});

  final ApiService api;
  final VoidCallback onSignedIn;

  @override
  State<AuthPage> createState() => _AuthPageState();
}

class _AuthPageState extends State<AuthPage> {
  final fullName = TextEditingController();
  final email = TextEditingController();
  final password = TextEditingController();
  var isRegister = false;
  var loading = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 430),
              child: AppCard(
                padding: const EdgeInsets.all(22),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Icon(Icons.local_parking, size: 58, color: pine),
                    const SizedBox(height: 12),
                    Text(
                      isRegister
                          ? 'Tạo tài khoản SmartParking'
                          : 'Đăng nhập SmartParking',
                      textAlign: TextAlign.center,
                      style:
                          Theme.of(context).textTheme.headlineSmall?.copyWith(
                                fontWeight: FontWeight.w800,
                                color: ink,
                              ),
                    ),
                    const SizedBox(height: 24),
                    if (isRegister) ...[
                      TextField(
                          controller: fullName,
                          decoration:
                              const InputDecoration(labelText: 'Họ tên')),
                      const SizedBox(height: 12),
                    ],
                    TextField(
                      controller: email,
                      decoration: const InputDecoration(labelText: 'Email'),
                      keyboardType: TextInputType.emailAddress,
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: password,
                      decoration: const InputDecoration(labelText: 'Mật khẩu'),
                      obscureText: true,
                    ),
                    const SizedBox(height: 18),
                    FilledButton.icon(
                      onPressed: loading ? null : submit,
                      icon: loading
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Icon(isRegister
                              ? Icons.person_add_alt_1
                              : Icons.login),
                      label: Text(isRegister ? 'Đăng ký' : 'Đăng nhập'),
                    ),
                    TextButton(
                      onPressed: loading
                          ? null
                          : () => setState(() => isRegister = !isRegister),
                      child: Text(isRegister
                          ? 'Đã có tài khoản? Đăng nhập'
                          : 'Chưa có tài khoản? Đăng ký'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> submit() async {
    setState(() => loading = true);
    try {
      if (isRegister) {
        await widget.api
            .register(fullName.text.trim(), email.text.trim(), password.text);
      } else {
        await widget.api.login(email.text.trim(), password.text);
      }
      if (!mounted) return;
      widget.onSignedIn();
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }
}

class HomeShell extends StatefulWidget {
  const HomeShell(
      {required this.api,
      required this.initialIndex,
      required this.onSignedOut,
      super.key});

  final ApiService api;
  final int initialIndex;
  final VoidCallback onSignedOut;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  late var index = widget.initialIndex;

  @override
  void didUpdateWidget(covariant HomeShell oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.initialIndex != oldWidget.initialIndex) {
      setState(() => index = widget.initialIndex);
    }
  }

  @override
  Widget build(BuildContext context) {
    final pages = [
      ProfilePage(api: widget.api, onSignedOut: widget.onSignedOut),
      TransactionsPage(api: widget.api),
      HistoryPage(api: widget.api),
      VehiclesPage(api: widget.api),
      VehicleLocationsPage(api: widget.api),
    ];
    return Scaffold(
      appBar: AppBar(title: const Text('SmartParking')),
      body: pages[index],
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: const [
          NavigationDestination(
              icon: Icon(Icons.person_outline),
              selectedIcon: Icon(Icons.person),
              label: 'Hồ sơ'),
          NavigationDestination(
              icon: Icon(Icons.receipt_long_outlined),
              selectedIcon: Icon(Icons.receipt_long),
              label: 'Giao dịch'),
          NavigationDestination(
              icon: Icon(Icons.history),
              selectedIcon: Icon(Icons.history),
              label: 'Lịch sử'),
          NavigationDestination(
              icon: Icon(Icons.directions_car_outlined),
              selectedIcon: Icon(Icons.directions_car),
              label: 'Xe'),
          NavigationDestination(
              icon: Icon(Icons.location_on_outlined),
              selectedIcon: Icon(Icons.location_on),
              label: 'Vị trí'),
        ],
      ),
    );
  }
}

class ProfilePage extends StatefulWidget {
  const ProfilePage({required this.api, required this.onSignedOut, super.key});

  final ApiService api;
  final VoidCallback onSignedOut;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  final fullName = TextEditingController();
  final phone = TextEditingController();
  AppUser? user;
  var loading = true;
  var saving = false;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      user = await widget.api.getProfile();
      fullName.text = user?.fullName ?? '';
      phone.text = user?.phoneNumber ?? '';
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    final activePasses =
        user?.monthlyPasses.where((item) => item.isValidNow).length ?? 0;
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const SectionHeader(
            title: 'Hồ sơ cá nhân',
          ),
          AppCard(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    CircleAvatar(
                      radius: 28,
                      backgroundColor: mint,
                      child: Text(
                        initials(user?.fullName ?? user?.email ?? 'SP'),
                        style: const TextStyle(
                            color: pine, fontWeight: FontWeight.w800),
                      ),
                    ),
                    const SizedBox(width: 14),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                              user?.fullName.isEmpty == true
                                  ? 'Người dùng SmartParking'
                                  : user?.fullName ?? '',
                              style: Theme.of(context)
                                  .textTheme
                                  .titleLarge
                                  ?.copyWith(fontWeight: FontWeight.w800)),
                        ],
                      ),
                    ),
                    StatusPill(
                        label: user?.isActive == true
                            ? 'Đang hoạt động'
                            : 'Tạm khóa',
                        color: user?.isActive == true ? pine : Colors.red),
                  ],
                ),
                const SizedBox(height: 18),
                TextField(
                    controller: fullName,
                    decoration: const InputDecoration(labelText: 'Họ tên')),
                const SizedBox(height: 12),
                TextField(
                  controller: phone,
                  decoration: const InputDecoration(labelText: 'Số điện thoại'),
                  keyboardType: TextInputType.phone,
                ),
                const SizedBox(height: 16),
                FilledButton.icon(
                  onPressed: saving ? null : save,
                  icon: saving
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.save_outlined),
                  label: const Text('Cập nhật thông tin'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              MetricTile(
                  label: 'Xe đã thêm',
                  value: '${user?.vehicles.length ?? 0}',
                  icon: Icons.directions_car),
              MetricTile(
                  label: 'Vé đang hiệu lực',
                  value: '$activePasses',
                  icon: Icons.confirmation_number),
            ],
          ),
          const SizedBox(height: 16),
          AppCard(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Vé tháng của tôi',
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.w800)),
                const SizedBox(height: 10),
                if (user?.monthlyPasses.isEmpty ?? true)
                  const EmptyText('Chưa có vé tháng nào.')
                else
                  for (final pass in user!.monthlyPasses.take(3))
                    CompactPassTile(pass: pass),
              ],
            ),
          ),
          const SizedBox(height: 16),
          OutlinedButton.icon(
            onPressed: () async {
              await widget.api.clearSession();
              widget.onSignedOut();
            },
            icon: const Icon(Icons.logout),
            label: const Text('Đăng xuất'),
          ),
        ],
      ),
    );
  }

  Future<void> save() async {
    setState(() => saving = true);
    try {
      await widget.api.updateProfile(fullName.text.trim(), phone.text.trim());
      await load();
      if (!mounted) return;
      showMessage(context, 'Đã cập nhật hồ sơ.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }
}

class TransactionsPage extends StatefulWidget {
  const TransactionsPage({required this.api, super.key});

  final ApiService api;

  @override
  State<TransactionsPage> createState() => _TransactionsPageState();
}

class _TransactionsPageState extends State<TransactionsPage> {
  List<WalletTransaction> transactions = [];
  var loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      transactions = await widget.api.getWalletTransactions();
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());

    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const SectionHeader(
            title: 'Giao dịch',
          ),
          if (transactions.isEmpty)
            const AppCard(
                padding: EdgeInsets.all(16),
                child: EmptyText('Chưa có giao dịch.'))
          else
            for (final item in transactions) TransactionTile(item: item),
        ],
      ),
    );
  }
}

class HistoryPage extends StatefulWidget {
  const HistoryPage({required this.api, super.key});

  final ApiService api;

  @override
  State<HistoryPage> createState() => _HistoryPageState();
}

class _HistoryPageState extends State<HistoryPage> {
  List<ParkingHistoryItem> items = [];
  var loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      items = await widget.api.getParkingHistory();
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const SectionHeader(title: 'Lịch sử gửi xe'),
          if (items.isEmpty)
            const AppCard(
                padding: EdgeInsets.all(18),
                child: EmptyText('Chưa có lịch sử gửi xe.'))
          else
            for (final item in items) ParkingHistoryTile(item: item),
        ],
      ),
    );
  }
}

class VehicleLocationsPage extends StatefulWidget {
  const VehicleLocationsPage({required this.api, super.key});

  final ApiService api;

  @override
  State<VehicleLocationsPage> createState() => _VehicleLocationsPageState();
}

class _VehicleLocationsPageState extends State<VehicleLocationsPage> {
  List<VehicleLocationInfo> locations = [];
  var loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      locations = await widget.api.getVehicleLocations();
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const SectionHeader(
            title: 'Vị trí xe',
            subtitle: 'Vị trí mới nhất được camera trong bãi ghi nhận.',
          ),
          if (locations.isEmpty)
            const AppCard(
              padding: EdgeInsets.all(18),
              child: EmptyText('Chưa có vị trí xe mới nhất.'),
            )
          else
            for (final item in locations) VehicleLocationTile(item: item),
        ],
      ),
    );
  }
}

class VehiclesPage extends StatefulWidget {
  const VehiclesPage({required this.api, super.key});

  final ApiService api;

  @override
  State<VehiclesPage> createState() => _VehiclesPageState();
}

class _VehiclesPageState extends State<VehiclesPage> {
  final plate = TextEditingController();
  final brand = TextEditingController();
  final color = TextEditingController();
  final ownerName = TextEditingController();
  final ownerPhone = TextEditingController();
  final defaultPlate = TextEditingController();
  var vehicleType = 'Motorbike';
  var defaultVehicle = true;
  var monthCount = 1;
  AppUser? user;
  List<VehicleInfo> vehicles = [];
  VehicleInfo? selectedVehicle;
  var loading = true;
  var savingVehicle = false;
  var buyingPass = false;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final results = await Future.wait(
          [widget.api.getProfile(), widget.api.getVehicles()]);
      user = results[0] as AppUser;
      vehicles = results[1] as List<VehicleInfo>;
      selectedVehicle = vehicles.where((item) => item.isDefault).firstOrNull ??
          (vehicles.isNotEmpty ? vehicles.first : null);
      fillMonthlyPassFields();
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  void fillMonthlyPassFields() {
    ownerName.text = user?.fullName ?? '';
    ownerPhone.text = user?.phoneNumber ?? '';
    defaultPlate.text = selectedVehicle?.licensePlate ?? '';
  }

  @override
  Widget build(BuildContext context) {
    if (loading) return const Center(child: CircularProgressIndicator());
    final from = DateTime.now();
    final to = DateTime(from.year, from.month + monthCount, from.day);
    final total = monthlyAmount * monthCount;

    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const SectionHeader(title: 'Xe của tôi'),
          AppCard(
            padding: const EdgeInsets.all(18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Thêm xe mới',
                    style: Theme.of(context)
                        .textTheme
                        .titleMedium
                        ?.copyWith(fontWeight: FontWeight.w800)),
                const SizedBox(height: 12),
                TextField(
                    controller: plate,
                    decoration: const InputDecoration(labelText: 'Biển số')),
                const SizedBox(height: 12),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(
                        value: 'Motorbike',
                        icon: Icon(Icons.two_wheeler),
                        label: Text('Xe máy')),
                    ButtonSegment(
                        value: 'Car',
                        icon: Icon(Icons.directions_car),
                        label: Text('Ô tô')),
                  ],
                  selected: {vehicleType},
                  onSelectionChanged: (value) =>
                      setState(() => vehicleType = value.first),
                ),
                const SizedBox(height: 12),
                TextField(
                    controller: brand,
                    decoration: const InputDecoration(labelText: 'Hãng xe')),
                const SizedBox(height: 12),
                TextField(
                    controller: color,
                    decoration: const InputDecoration(labelText: 'Màu xe')),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  value: defaultVehicle,
                  onChanged: (value) => setState(() => defaultVehicle = value),
                  title: const Text('Đặt làm xe mặc định'),
                ),
                FilledButton.icon(
                  onPressed: savingVehicle ? null : addVehicle,
                  icon: savingVehicle
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.add),
                  label: const Text('Thêm xe'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 18),
          if (vehicles.isEmpty)
            const AppCard(
                padding: EdgeInsets.all(18),
                child: EmptyText('Bạn cần thêm xe trước khi mua vé tháng.'))
          else ...[
            Text('Danh sách xe',
                style: Theme.of(context)
                    .textTheme
                    .titleMedium
                    ?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 8),
            for (final vehicle in vehicles)
              VehicleTile(
                vehicle: vehicle,
                selected: selectedVehicle?.id == vehicle.id,
                onSelect: () => setState(() {
                  selectedVehicle = vehicle;
                  fillMonthlyPassFields();
                }),
                onSetDefault: () => setDefaultVehicle(vehicle),
                onDelete: () => deleteVehicle(vehicle),
              ),
            const SizedBox(height: 18),
            AppCard(
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.confirmation_number, color: pine),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text('Mua vé tháng cho xe mặc định',
                            style: Theme.of(context)
                                .textTheme
                                .titleMedium
                                ?.copyWith(fontWeight: FontWeight.w800)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  TextField(
                      controller: ownerName,
                      decoration:
                          const InputDecoration(labelText: 'Tên chủ xe')),
                  const SizedBox(height: 12),
                  TextField(
                    controller: ownerPhone,
                    decoration: const InputDecoration(
                        labelText: 'Số điện thoại chủ xe'),
                    keyboardType: TextInputType.phone,
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: defaultPlate,
                    enabled: false,
                    decoration:
                        const InputDecoration(labelText: 'Biển số xe mặc định'),
                  ),
                  const SizedBox(height: 8),
                  StepperControl(
                    label: 'Số tháng',
                    value: monthCount,
                    onChanged: (value) =>
                        setState(() => monthCount = value.clamp(1, 12)),
                  ),
                  ReadOnlyLine(
                      label: 'Hiệu lực',
                      value:
                          '${shortDateFormat.format(from)} - ${shortDateFormat.format(to)}'),
                  ReadOnlyLine(
                      label: 'Tổng thanh toán',
                      value: currencyFormat.format(total)),
                  const SizedBox(height: 12),
                  FilledButton.icon(
                    onPressed: buyingPass ? null : buyPassForDefaultVehicle,
                    icon: buyingPass
                        ? const SizedBox(
                            height: 18,
                            width: 18,
                            child: CircularProgressIndicator(strokeWidth: 2))
                        : const Icon(Icons.payment),
                    label: const Text('Mua vé tháng qua MoMo'),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> addVehicle() async {
    setState(() => savingVehicle = true);
    try {
      await widget.api.createVehicle(
        licensePlate: plate.text,
        vehicleType: vehicleType,
        brand: brand.text.trim(),
        color: color.text.trim(),
        isDefault: defaultVehicle,
      );
      plate.clear();
      brand.clear();
      color.clear();
      await load();
      if (!mounted) return;
      showMessage(context, 'Đã thêm xe.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => savingVehicle = false);
    }
  }

  Future<void> setDefaultVehicle(VehicleInfo vehicle) async {
    try {
      await widget.api.updateVehicle(
        id: vehicle.id,
        brand: vehicle.brand,
        color: vehicle.color,
        isDefault: true,
      );
      await load();
      if (!mounted) return;
      showMessage(context, 'Đã chọn xe mặc định.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
  }

  Future<void> deleteVehicle(VehicleInfo vehicle) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xóa xe đã đăng ký?'),
        content: Text(
          'Xe ${vehicle.licensePlate} chỉ được xóa nếu chưa mua vé tháng hoặc vé tháng đã hết hiệu lực.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Hủy'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xóa'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    try {
      await widget.api.deleteVehicle(vehicle.id);
      await load();
      if (!mounted) return;
      showMessage(context, 'Đã xóa xe.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
  }

  Future<void> buyPassForDefaultVehicle() async {
    final vehicle = selectedVehicle;
    if (vehicle == null) {
      showMessage(context, 'Vui lòng chọn xe mặc định.');
      return;
    }
    if (ownerName.text.trim().isEmpty || ownerPhone.text.trim().isEmpty) {
      showMessage(
          context, 'Vui lòng cập nhật đầy đủ tên và số điện thoại chủ xe.');
      return;
    }
    setState(() => buyingPass = true);
    try {
      final from = DateTime.now();
      final to = DateTime(from.year, from.month + monthCount, from.day);
      final payment = await widget.api.createMonthlyPassMomoPayment(
        amount: monthlyAmount * monthCount,
        paymentMethod: 'Wallet',
        licensePlate: vehicle.licensePlate,
        ownerName: ownerName.text,
        ownerPhone: ownerPhone.text,
        validFrom: from,
        validTo: to,
      );
      final rawUrl =
          payment['deeplink'] ?? payment['payUrl'] ?? payment['qrCodeUrl'];
      if (rawUrl == null || rawUrl.toString().isEmpty) {
        if (!mounted) return;
        showMessage(context,
            payment['message']?.toString() ?? 'Không nhận được link MoMo.');
        return;
      }
      await launchUrl(Uri.parse(rawUrl.toString()),
          mode: LaunchMode.externalApplication);
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    } finally {
      if (mounted) setState(() => buyingPass = false);
    }
  }
}

const monthlyAmount = 200000.0;

class AppCard extends StatelessWidget {
  const AppCard(
      {required this.child, this.padding = EdgeInsets.zero, super.key});

  final Widget child;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Card(child: Padding(padding: padding, child: child));
  }
}

class SectionHeader extends StatelessWidget {
  const SectionHeader({required this.title, this.subtitle, super.key});

  final String title;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(title,
              style: Theme.of(context)
                  .textTheme
                  .headlineSmall
                  ?.copyWith(fontWeight: FontWeight.w800, color: ink)),
          if (subtitle != null) ...[
            const SizedBox(height: 4),
            Text(subtitle!,
                style: Theme.of(context)
                    .textTheme
                    .bodyMedium
                    ?.copyWith(color: Colors.black54)),
          ],
        ],
      ),
    );
  }
}

class StatusPill extends StatelessWidget {
  const StatusPill({required this.label, required this.color, super.key});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .10),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(label,
          style: TextStyle(
              color: color, fontSize: 12, fontWeight: FontWeight.w800)),
    );
  }
}

class MetricTile extends StatelessWidget {
  const MetricTile(
      {required this.label,
      required this.value,
      required this.icon,
      super.key});

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 165,
      child: AppCard(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: pine),
            const SizedBox(height: 10),
            Text(value,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style:
                    const TextStyle(fontWeight: FontWeight.w900, fontSize: 18)),
            Text(label,
                style: const TextStyle(color: Colors.black54, fontSize: 12)),
          ],
        ),
      ),
    );
  }
}

class CompactPassTile extends StatelessWidget {
  const CompactPassTile({required this.pass, super.key});

  final MonthlyPassInfo pass;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Icon(pass.isValidNow ? Icons.verified : Icons.event_busy,
          color: pass.isValidNow ? pine : Colors.black45),
      title: Text(pass.licensePlate,
          style: const TextStyle(fontWeight: FontWeight.w800)),
      subtitle: Text(
          '${shortDateFormat.format(pass.validFrom)} - ${shortDateFormat.format(pass.validTo)}'),
      trailing: StatusPill(
          label: pass.isValidNow ? 'Hiệu lực' : 'Hết hạn',
          color: pass.isValidNow ? pine : Colors.black45),
    );
  }
}

class TransactionTile extends StatelessWidget {
  const TransactionTile({required this.item, super.key});

  final WalletTransaction item;

  @override
  Widget build(BuildContext context) {
    final positive = item.amount >= 0;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: AppCard(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: ListTile(
          contentPadding: EdgeInsets.zero,
          leading: CircleAvatar(
            backgroundColor: positive ? mint : const Color(0xFFFFECEC),
            child: Icon(positive ? Icons.add : Icons.remove,
                color: positive ? pine : Colors.red),
          ),
          title: Text(item.description.isEmpty
              ? 'Thanh toán SmartParking'
              : item.description),
          subtitle: Text(dateFormat.format(item.createdAt)),
          trailing: Text(currencyFormat.format(item.amount),
              style: const TextStyle(fontWeight: FontWeight.w800)),
        ),
      ),
    );
  }
}

class ParkingHistoryTile extends StatelessWidget {
  const ParkingHistoryTile({required this.item, super.key});

  final ParkingHistoryItem item;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: AppCard(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: ListTile(
          contentPadding: EdgeInsets.zero,
          leading: const CircleAvatar(
              backgroundColor: mint,
              child: Icon(Icons.local_parking, color: pine)),
          title: Text(item.licensePlate,
              style: const TextStyle(fontWeight: FontWeight.w800)),
          subtitle: Text(
            '${dateFormat.format(item.checkInTime)}'
            '${item.checkOutTime == null ? '' : '\n${dateFormat.format(item.checkOutTime!)}'}',
          ),
          trailing: Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(item.status,
                  style: const TextStyle(fontWeight: FontWeight.w700)),
              Text(currencyFormat.format(item.feeAmount ?? 0)),
            ],
          ),
        ),
      ),
    );
  }
}

class VehicleLocationTile extends StatelessWidget {
  const VehicleLocationTile({required this.item, super.key});

  final VehicleLocationInfo item;

  @override
  Widget build(BuildContext context) {
    final imageBytes =
        decodeImage(item.fullFrameImageBase64 ?? item.imageBase64);
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: AppCard(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const CircleAvatar(
                    backgroundColor: mint,
                    child: Icon(Icons.location_on, color: pine)),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.licensePlate,
                          style: const TextStyle(
                              fontWeight: FontWeight.w900, fontSize: 16)),
                      Text(item.locationName,
                          style: const TextStyle(fontWeight: FontWeight.w700)),
                    ],
                  ),
                ),
                StatusPill(
                    label: '${(item.confidence * 100).round()}%', color: pine),
              ],
            ),
            const SizedBox(height: 10),
            ReadOnlyLine(label: 'Camera', value: item.cameraId),
            ReadOnlyLine(
                label: 'Thời gian', value: dateFormat.format(item.detectedAt)),
            if (item.zoneCode?.isNotEmpty == true ||
                item.columnCode?.isNotEmpty == true)
              ReadOnlyLine(
                label: 'Khu vuc',
                value: [
                  if (item.parkingLotCode?.isNotEmpty == true)
                    item.parkingLotCode!,
                  if (item.zoneCode?.isNotEmpty == true) item.zoneCode!,
                  if (item.columnCode?.isNotEmpty == true) item.columnCode!,
                ].join(' - '),
              ),
            if (imageBytes != null) ...[
              const SizedBox(height: 10),
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.memory(
                  imageBytes,
                  height: 180,
                  fit: BoxFit.cover,
                  gaplessPlayback: true,
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class VehicleTile extends StatelessWidget {
  const VehicleTile({
    required this.vehicle,
    required this.selected,
    required this.onSelect,
    required this.onSetDefault,
    required this.onDelete,
    super.key,
  });

  final VehicleInfo vehicle;
  final bool selected;
  final VoidCallback onSelect;
  final VoidCallback onSetDefault;
  final VoidCallback onDelete;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: AppCard(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        child: ListTile(
          contentPadding: EdgeInsets.zero,
          onTap: onSelect,
          leading: CircleAvatar(
            backgroundColor: selected ? mint : const Color(0xFFF0F1F0),
            child: Icon(
              vehicle.vehicleType == 'Car'
                  ? Icons.directions_car
                  : Icons.two_wheeler,
              color: selected ? pine : Colors.black54,
            ),
          ),
          title: Text(vehicle.licensePlate,
              style: const TextStyle(fontWeight: FontWeight.w800)),
          subtitle: Text('${vehicle.brand} - ${vehicle.color}'),
          trailing: Wrap(
            spacing: 4,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              if (vehicle.isDefault)
                const StatusPill(label: 'Mặc định', color: pine)
              else
                TextButton.icon(
                  onPressed: onSetDefault,
                  icon: const Icon(Icons.star_outline),
                  label: const Text('Chọn'),
                ),
              IconButton(
                tooltip: 'Xóa xe',
                onPressed: onDelete,
                icon: const Icon(Icons.delete_outline),
                color: Colors.redAccent,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class StepperControl extends StatelessWidget {
  const StepperControl({
    required this.label,
    required this.value,
    required this.onChanged,
    super.key,
  });

  final String label;
  final int value;
  final ValueChanged<int> onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
            child: Text('$label: $value',
                style: const TextStyle(fontWeight: FontWeight.w700))),
        IconButton(
          tooltip: 'Giảm',
          onPressed: () => onChanged(value - 1),
          icon: const Icon(Icons.remove_circle_outline),
        ),
        IconButton(
          tooltip: 'Tăng',
          onPressed: () => onChanged(value + 1),
          icon: const Icon(Icons.add_circle_outline),
        ),
      ],
    );
  }
}

class ReadOnlyLine extends StatelessWidget {
  const ReadOnlyLine({required this.label, required this.value, super.key});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        children: [
          Expanded(
              child:
                  Text(label, style: const TextStyle(color: Colors.black54))),
          Flexible(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
          ),
        ],
      ),
    );
  }
}

class EmptyText extends StatelessWidget {
  const EmptyText(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(text,
        textAlign: TextAlign.center,
        style: const TextStyle(color: Colors.black54));
  }
}

extension FirstOrNull<T> on Iterable<T> {
  T? get firstOrNull => isEmpty ? null : first;
}

String initials(String value) {
  final parts = value
      .trim()
      .split(RegExp(r'\s+'))
      .where((item) => item.isNotEmpty)
      .toList();
  if (parts.isEmpty) return 'SP';
  if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
  return '${parts.first.substring(0, 1)}${parts.last.substring(0, 1)}'
      .toUpperCase();
}

Uint8List? decodeImage(String? value) {
  if (value == null || value.isEmpty) return null;
  try {
    return base64Decode(value);
  } catch (_) {
    return null;
  }
}

void showMessage(BuildContext context, String message) {
  if (!context.mounted) return;
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}
