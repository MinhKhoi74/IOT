import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:url_launcher/url_launcher.dart';

import 'models/app_models.dart';
import 'services/api_service.dart';

void main() => runApp(const SmartParkingUserApp());

final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'VND');
final dateFormat = DateFormat('dd/MM/yyyy HH:mm');
final shortDateFormat = DateFormat('dd/MM/yyyy');

class SmartParkingUserApp extends StatefulWidget {
  const SmartParkingUserApp({super.key});

  @override
  State<SmartParkingUserApp> createState() => _SmartParkingUserAppState();
}

class _SmartParkingUserAppState extends State<SmartParkingUserApp> {
  final api = ApiService();
  var ready = false;

  @override
  void initState() {
    super.initState();
    api.loadSession().whenComplete(() => setState(() => ready = true));
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'SmartParking',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF0E7C66)),
        useMaterial3: true,
        inputDecorationTheme: const InputDecorationTheme(border: OutlineInputBorder()),
      ),
      home: !ready
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : api.isSignedIn
              ? HomeShell(api: api, onSignedOut: _signedOut)
              : AuthPage(api: api, onSignedIn: _signedIn),
    );
  }

  void _signedIn() => setState(() {});
  void _signedOut() => setState(() {});
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
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(Icons.local_parking, size: 64),
                  const SizedBox(height: 12),
                  Text(
                    isRegister ? 'Tao tai khoan SmartParking' : 'Dang nhap SmartParking',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 24),
                  if (isRegister) ...[
                    TextField(controller: fullName, decoration: const InputDecoration(labelText: 'Ho ten')),
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
                    decoration: const InputDecoration(labelText: 'Mat khau'),
                    obscureText: true,
                  ),
                  const SizedBox(height: 20),
                  FilledButton.icon(
                    onPressed: loading ? null : submit,
                    icon: loading
                        ? const SizedBox(
                            height: 18,
                            width: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : Icon(isRegister ? Icons.person_add : Icons.login),
                    label: Text(isRegister ? 'Dang ky' : 'Dang nhap'),
                  ),
                  InkWell(
                    onTap: loading
                        ? null
                        : () => setState(() => isRegister = !isRegister),
                    child: Padding(
                      padding: const EdgeInsets.all(8),
                      child: Center(
                        child: Text(isRegister
                            ? 'Da co tai khoan? Dang nhap'
                            : 'Chua co tai khoan? Dang ky'),
                      ),
                    ),
                  ),
                ],
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
        await widget.api.register(fullName.text.trim(), email.text.trim(), password.text);
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
  const HomeShell({required this.api, required this.onSignedOut, super.key});

  final ApiService api;
  final VoidCallback onSignedOut;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  var index = 0;

  @override
  Widget build(BuildContext context) {
    final pages = [
      ProfilePage(api: widget.api, onSignedOut: widget.onSignedOut),
      WalletPage(api: widget.api),
      HistoryPage(api: widget.api),
      VehiclesPage(api: widget.api),
    ];
    return Scaffold(
      appBar: AppBar(title: const Text('SmartParking')),
      body: pages[index],
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.person_outline), label: 'Ho so'),
          NavigationDestination(icon: Icon(Icons.account_balance_wallet_outlined), label: 'Vi'),
          NavigationDestination(icon: Icon(Icons.history), label: 'Lich su'),
          NavigationDestination(icon: Icon(Icons.directions_car_outlined), label: 'Xe'),
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
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Ho so ca nhan', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 16),
          TextField(controller: fullName, decoration: const InputDecoration(labelText: 'Ho ten')),
          const SizedBox(height: 12),
          TextField(
            controller: phone,
            decoration: const InputDecoration(labelText: 'So dien thoai'),
            keyboardType: TextInputType.phone,
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: false,
            decoration: InputDecoration(labelText: 'Email', hintText: user?.email),
          ),
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: save,
            icon: const Icon(Icons.save_outlined),
            label: const Text('Luu thong tin'),
          ),
          OutlinedButton.icon(
            onPressed: () async {
              await widget.api.clearSession();
              widget.onSignedOut();
            },
            icon: const Icon(Icons.logout),
            label: const Text('Dang xuat'),
          ),
        ],
      ),
    );
  }

  Future<void> save() async {
    try {
      await widget.api.updateProfile(fullName.text.trim(), phone.text.trim());
      await load();
      if (!mounted) return;
      showMessage(context, 'Da cap nhat ho so.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
  }
}

class WalletPage extends StatefulWidget {
  const WalletPage({required this.api, super.key});

  final ApiService api;

  @override
  State<WalletPage> createState() => _WalletPageState();
}

class _WalletPageState extends State<WalletPage> {
  final amount = TextEditingController(text: '50000');
  WalletInfo? wallet;
  List<WalletTransaction> transactions = [];
  var method = 'Wallet';
  var loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      wallet = await widget.api.getWallet();
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
          Text('So du vi', style: Theme.of(context).textTheme.titleMedium),
          Text(
            currencyFormat.format(wallet?.balance ?? 0),
            style: Theme.of(context).textTheme.headlineMedium,
          ),
          const SizedBox(height: 20),
          TextField(
            controller: amount,
            decoration: const InputDecoration(labelText: 'So tien nap'),
            keyboardType: TextInputType.number,
          ),
          const SizedBox(height: 12),
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'Wallet', label: Text('MoMo')),
              ButtonSegment(value: 'ATM', label: Text('ATM')),
            ],
            selected: {method},
            onSelectionChanged: (value) => setState(() => method = value.first),
          ),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: topUp,
            icon: const Icon(Icons.payment),
            label: const Text('Nap tien qua MoMo'),
          ),
          const SizedBox(height: 24),
          Text('Giao dich gan day', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          for (final item in transactions)
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: CircleAvatar(child: Icon(item.amount >= 0 ? Icons.add : Icons.remove)),
              title: Text(item.description),
              subtitle: Text(dateFormat.format(item.createdAt)),
              trailing: Text(currencyFormat.format(item.amount)),
            ),
        ],
      ),
    );
  }

  Future<void> topUp() async {
    try {
      final value = double.tryParse(amount.text.trim()) ?? 0;
      final payment = await widget.api.createMomoTopUp(value, method);
      final rawUrl = payment['deeplink'] ?? payment['payUrl'] ?? payment['qrCodeUrl'];
      if (rawUrl == null || rawUrl.toString().isEmpty) {
        if (!mounted) return;
        showMessage(context, payment['message']?.toString() ?? 'Khong nhan duoc link MoMo.');
        return;
      }
      await launchUrl(Uri.parse(rawUrl.toString()), mode: LaunchMode.externalApplication);
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
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
      child: items.isEmpty
          ? ListView(
              padding: const EdgeInsets.all(24),
              children: const [Center(child: Text('Chua co lich su gui xe.'))],
            )
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              separatorBuilder: (_, __) => const Divider(),
              itemBuilder: (context, index) {
                final item = items[index];
                return ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const CircleAvatar(child: Icon(Icons.local_parking)),
                  title: Text(item.licensePlate),
                  subtitle: Text(
                    '${dateFormat.format(item.checkInTime)}'
                    '${item.checkOutTime == null ? '' : ' - ${dateFormat.format(item.checkOutTime!)}'}',
                  ),
                  trailing: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(item.status),
                      Text(currencyFormat.format(item.feeAmount ?? 0)),
                    ],
                  ),
                );
              },
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
  var vehicleType = 'Motorbike';
  var defaultVehicle = true;
  var monthCount = 1;
  List<VehicleInfo> vehicles = [];
  var loading = true;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      vehicles = await widget.api.getVehicles();
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
    final from = DateTime.now();
    final to = DateTime(from.year, from.month + monthCount, from.day);
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Dang ky xe', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 12),
          TextField(controller: plate, decoration: const InputDecoration(labelText: 'Bien so')),
          const SizedBox(height: 12),
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'Motorbike', label: Text('Xe may')),
              ButtonSegment(value: 'Car', label: Text('O to')),
            ],
            selected: {vehicleType},
            onSelectionChanged: (value) => setState(() => vehicleType = value.first),
          ),
          const SizedBox(height: 12),
          TextField(controller: brand, decoration: const InputDecoration(labelText: 'Hang xe')),
          const SizedBox(height: 12),
          TextField(controller: color, decoration: const InputDecoration(labelText: 'Mau xe')),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            value: defaultVehicle,
            onChanged: (value) => setState(() => defaultVehicle = value),
            title: const Text('Dat lam xe mac dinh'),
          ),
          FilledButton.icon(
            onPressed: addVehicle,
            icon: const Icon(Icons.add),
            label: const Text('Them xe'),
          ),
          const SizedBox(height: 24),
          Text('Dang ky ve thang', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 12),
          TextField(controller: ownerName, decoration: const InputDecoration(labelText: 'Ten chu xe')),
          const SizedBox(height: 12),
          TextField(
            controller: ownerPhone,
            decoration: const InputDecoration(labelText: 'So dien thoai chu xe'),
            keyboardType: TextInputType.phone,
          ),
          StepperControl(
            label: 'So thang',
            value: monthCount,
            onChanged: (value) => setState(() => monthCount = value.clamp(1, 12)),
          ),
          Text('Hieu luc: ${shortDateFormat.format(from)} - ${shortDateFormat.format(to)}'),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: registerPass,
            icon: const Icon(Icons.confirmation_number_outlined),
            label: const Text('Dang ky ve thang'),
          ),
          const SizedBox(height: 24),
          Text('Xe cua toi', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          for (final vehicle in vehicles)
            ListTile(
              contentPadding: EdgeInsets.zero,
              leading: Icon(vehicle.vehicleType == 'Car' ? Icons.directions_car : Icons.two_wheeler),
              title: Text(vehicle.licensePlate),
              subtitle: Text('${vehicle.brand} - ${vehicle.color}'),
              trailing: vehicle.isDefault ? const Icon(Icons.star) : null,
              onTap: () => plate.text = vehicle.licensePlate,
            ),
        ],
      ),
    );
  }

  Future<void> addVehicle() async {
    try {
      await widget.api.createVehicle(
        licensePlate: plate.text,
        vehicleType: vehicleType,
        brand: brand.text.trim(),
        color: color.text.trim(),
        isDefault: defaultVehicle,
      );
      await load();
      if (!mounted) return;
      showMessage(context, 'Da them xe.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
  }

  Future<void> registerPass() async {
    final from = DateTime.now();
    final to = DateTime(from.year, from.month + monthCount, from.day);
    try {
      await widget.api.registerMonthlyPass(
        licensePlate: plate.text,
        ownerName: ownerName.text.trim(),
        ownerPhone: ownerPhone.text.trim(),
        validFrom: from,
        validTo: to,
      );
      if (!mounted) return;
      showMessage(context, 'Da dang ky ve thang.');
    } catch (error) {
      if (!mounted) return;
      showMessage(context, error.toString());
    }
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
        Expanded(child: Text('$label: $value')),
        IconButton(onPressed: () => onChanged(value - 1), icon: const Icon(Icons.remove_circle_outline)),
        IconButton(onPressed: () => onChanged(value + 1), icon: const Icon(Icons.add_circle_outline)),
      ],
    );
  }
}

void showMessage(BuildContext context, String message) {
  if (!context.mounted) return;
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}
