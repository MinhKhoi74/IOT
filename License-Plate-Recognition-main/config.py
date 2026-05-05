"""
Smart Parking AI Service Configuration
Tối ưu cho FPS thấp < 5fps
"""

# =====================
# API Configuration
# =====================
# Cổng 5000 = HTTP (mặc định .NET), 5001 = HTTPS
BACKEND_API_URL = "http://localhost:5000/api/parking"
BACKEND_BASE_URL = "http://localhost:5000"  # Base URL for health checks & endpoints
API_TIMEOUT = 10  # seconds

# =====================
# Transport Mode
# =====================
USE_WEBSOCKET = False  # False = HTTP with retry (more stable), True = WebSocket (experimental)
WEBSOCKET_RECONNECT_DELAY = 3  # seconds (if USE_WEBSOCKET=True)
WEBSOCKET_HEARTBEAT_INTERVAL = 30  # seconds (if USE_WEBSOCKET=True)

# =====================
# Camera Configuration
# =====================
# IPWebcam - sử dụng ứng dụng IP Webcam trên điện thoại
CAMERA_SOURCE = "http://192.168.1.20:8080/video"  # Đổi thành IP điện thoại của bạn
CAMERA_FRAME_WIDTH = 640
CAMERA_FRAME_HEIGHT = 480
CAMERA_FPS = 5
CAMERA_TIMEOUT = 10                 # Timeout kết nối (giây)

# =====================
# Preprocessing
# =====================
CAMERA_RESIZE_SCALE = 0.3  # 0.2-0.4 để giảm tải CPU (FPS thấp)
ENABLE_DISK_SAVE = False   # KHÔNG ghi file crop.jpg - truyền trực tiếp RAM

# =====================
# Realtime Tuning
# =====================
OCR_MIN_INTERVAL_ACTIVE = 0.1   # giây (khi đang scan)
OCR_MIN_INTERVAL_LOCKED = 1.0   # giây (khi đã chốt)
PLATE_ABSENCE_RESET_SECONDS = 1.2  # giây (biển số biến mất -> bật scan lại)
MAX_DETECTIONS_PER_FRAME = 1
MAX_OCR_PLATES_PER_FRAME_ACTIVE = 1
MAX_OCR_PLATES_PER_FRAME_LOCKED = 1

PLATE_DETECTOR_IMGSZ = 320  # 320/416/640 (nhỏ hơn = nhanh hơn)

# Deskew combinations for OCR (ít hơn = nhanh hơn, nhưng có thể giảm chính xác)
OCR_DESKEW_COMBOS = [(0, 0), (1, 0)]

# =====================
# Voting System (Buffer)
# =====================
# Flow mới:

PLATE_BUFFER_SIZE = 5
PLATE_BUFFER_WINDOW_SECONDS = 0.0  # 0 = không dùng time-window (vote theo đúng N mẫu gần nhất)
PLATE_VOTE_MIN_OCCURRENCES = 3     # 5/7
PLATE_VOTE_MIN_RATIO = (3.0 / 5.0)  # giữ để tương thích
PLATE_VOTE_REQUIRE_VN_FORMAT = False
VOTING_THRESHOLD = 0.0
PLATE_VOTE_FUZZY_GROUPING = False
PLATE_VOTE_FUZZY_THRESHOLD = 80.0

# Checkin 1 lần, bỏ qua lặp lại cho đến khi Checkout
CHECKIN_LOCK_ENABLED = True

# Hybrid gate: chốt ngay nếu có N mẫu giống nhau liên tiếp (fallback vẫn vote theo cửa sổ 7 mẫu)
HYBRID_CONSECUTIVE_REQUIRED = 3

# =====================
# Fuzzy Matching (Noise Tolerance)
# =====================
FUZZY_MATCH_THRESHOLD = 80.0  # %

# =====================
# Cooldown (Prevent Duplicate)
# =====================
COOLDOWN_SECONDS = 12
COOLDOWN_ENABLED = False  # Flow mới dùng checkin-lock thay cooldown

# =====================
# Model Configuration
# =====================
PLATE_DETECTOR_MODEL = "model/LP_detector_nano_61.pt"
OCR_MODEL = "model/LP_ocr_nano_62.pt"
OCR_CONFIDENCE = 0.55

# =====================
# Vietnam Plate Format Validation
# =====================
VIETNAM_PLATE_REGEX = r"^[0-9]{1,2}[A-Z]{1,2}[0-9]{0,2}-?\d{3,5}(?:\.\d{2,3})?$"

# =====================
# Station Configuration
# =====================
STATION_ENTRANCE = "STATION_01"
STATION_EXIT = "STATION_02"

# =====================
# Logging
# =====================
LOG_LEVEL = "INFO"
LOG_FORMAT = "[%(asctime)s] [%(name)s] [%(levelname)s] %(message)s"
LOG_DIR = "logs"

# =====================
# Persistent State (JSON) - shared between checkin/checkout services
# =====================
STATE_FILE_PATH = "logs/parking_state.json"
STATE_LOCK_TIMEOUT_SECONDS = 3.0
STATE_LOCK_RETRY_INTERVAL_SECONDS = 0.05

# =====================
# Debug
# =====================
SIMULATE_API = False  # True = test không cần backend
DEBUG_MODE = False
