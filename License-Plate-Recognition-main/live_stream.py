#!/usr/bin/env python3
"""
Live Stream Video từ IP Camera - Low Latency
Hiển thị video real-time từ camera điện thoại với độ trễ thấp

Usage:
    python live_stream.py --ip 192.168.1.20 --port 8080
    python live_stream.py --ip 192.168.1.20 --port 8080 --no-record
"""

import cv2
import argparse
import time
import sys
import threading
import logging
from collections import deque
from pathlib import Path

# =====================
# Setup Logging
# =====================
logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

# =====================
# Low-Latency Video Capture
# =====================

class LowLatencyCapture:
    """Video capture với buffer thấp để giảm latency."""
    
    def __init__(self, source, buffer_size=1):
        """
        Args:
            source: Camera source (0 hoặc URL)
            buffer_size: Số frames trong buffer (nhỏ hơn = latency thấp hơn)
        """
        self.cap = cv2.VideoCapture(source)
        
        # ===== Cấu hình để giảm latency =====
        
        # 1. Giảm buffer size (quan trọng!)
        self.cap.set(cv2.CAP_PROP_BUFFERSIZE, buffer_size)
        
        # 2. Thay đổi fourcc codec (nếu có thể)
        try:
            # MJPEG codec = low latency
            fourcc = cv2.VideoWriter_fourcc(*'MJPG')
            self.cap.set(cv2.CAP_PROP_FOURCC, fourcc)
        except:
            pass
        
        # 3. Giảm độ phân giải để nhanh hơn
        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
        
        # 4. Giảm FPS nếu cần (tùy app)
        try:
            self.cap.set(cv2.CAP_PROP_FPS, 30)
        except:
            pass
        
        if not self.cap.isOpened():
            raise RuntimeError("Failed to open camera")
        
        logger.info("✓ Camera initialized (low-latency mode)")
        
        # Frame queue
        self.frame_queue = deque(maxlen=2)  # Giữ max 2 frames
        self.running = True
        
        # Thread để capture liên tục
        self.capture_thread = threading.Thread(target=self._capture_loop, daemon=True)
        self.capture_thread.start()
        
        time.sleep(0.5)  # Đợi thread khởi động
    
    def _capture_loop(self):
        """Background thread để capture frames."""
        while self.running:
            ret, frame = self.cap.read()
            if ret:
                self.frame_queue.append((time.time(), frame))
            else:
                time.sleep(0.01)
    
    def read(self):
        """Lấy frame mới nhất (không chờ)."""
        if self.frame_queue:
            return self.frame_queue[-1]  # Lấy frame mới nhất
        return None, None
    
    def release(self):
        """Đóng camera."""
        self.running = False
        self.cap.release()


# =====================
# Main Live Stream
# =====================

def main():
    """Main entry point."""
    
    ap = argparse.ArgumentParser(
        description='Live stream video từ IP camera - Low latency'
    )
    ap.add_argument('--ip', type=str, default='192.168.1.20',
                   help='Phone IP address (default: 192.168.1.20)')
    ap.add_argument('--port', type=int, default=8080,
                   help='IP Camera port (default: 8080)')
    ap.add_argument('--scale', type=float, default=0.8,
                   help='Display scale (0-1, nhỏ hơn = nhanh hơn)')
    ap.add_argument('--no-record', action='store_true',
                   help='Do not record video')
    
    args = ap.parse_args()
    
    # =====================
    # Camera Source
    # =====================
    camera_url = f'http://{args.ip}:{args.port}/video'
    logger.info(f"Connecting to: {camera_url}")
    logger.info("")
    
    try:
        cap = LowLatencyCapture(camera_url, buffer_size=1)
    except Exception as e:
        logger.error(f"Failed to connect: {str(e)}")
        logger.error("Check IP address and IP Webcam app is running")
        sys.exit(1)
    
    # =====================
    # Video Writer (Optional)
    # =====================
    out = None
    if not args.no_record:
        fourcc = cv2.VideoWriter_fourcc(*'mp4v')
        timestamp = int(time.time())
        output_file = f"video_{timestamp}.mp4"
        out = cv2.VideoWriter(output_file, fourcc, 30.0, (640, 480))
        logger.info(f"Recording to: {output_file}")
    
    # =====================
    # Display
    # =====================
    logger.info("🎬 Starting live stream...")
    logger.info("Controls: q=quit, s=screenshot, r=toggle record")
    logger.info("=" * 70)
    
    frame_count = 0
    start_time = time.time()
    latest_timestamp = None
    avg_latency = deque(maxlen=30)  # 30 frame avg
    recording = not args.no_record
    
    while True:
        # Get frame
        timestamp, frame = cap.read()
        
        if frame is None:
            logger.warning("⚠️  No frame available")
            time.sleep(0.1)
            continue
        
        frame_count += 1
        
        # === Calculate latency ===
        if timestamp and latest_timestamp:
            latency_ms = (time.time() - timestamp) * 1000
            avg_latency.append(latency_ms)
        
        latest_timestamp = timestamp
        
        # === Resize for display ===
        h, w = frame.shape[:2]
        display_frame = cv2.resize(frame, 
                                  (int(w * args.scale), int(h * args.scale)))
        
        # === Calculate FPS ===
        elapsed = time.time() - start_time
        fps = frame_count / elapsed if elapsed > 0 else 0
        
        # === Add overlay text ===
        text_color = (0, 255, 0)  # Green
        
        # Frame count
        cv2.putText(display_frame, f"Frame: {frame_count}", (10, 30),
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, text_color, 2)
        
        # FPS
        cv2.putText(display_frame, f"FPS: {fps:.1f}", (10, 70),
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, text_color, 2)
        
        # Latency
        if avg_latency:
            avg_lat = sum(avg_latency) / len(avg_latency)
            cv2.putText(display_frame, f"Latency: {avg_lat:.0f}ms", (10, 110),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.7, text_color, 2)
        
        # Recording status
        if recording:
            cv2.putText(display_frame, "REC", (10, 150),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
        
        # === Display frame ===
        cv2.imshow(f"Live Stream - {args.ip}:{args.port} [q=quit, s=screenshot, r=record]",
                  display_frame)
        
        # === Record ===
        if recording and out is not None:
            out.write(frame)
        
        # === Keyboard input ===
        key = cv2.waitKey(1) & 0xFF
        
        if key == ord('q'):
            logger.info("✓ User quit")
            break
        elif key == ord('s'):
            filename = f"screenshot_{int(time.time())}.jpg"
            cv2.imwrite(filename, frame)
            logger.info(f"✓ Saved: {filename}")
        elif key == ord('r'):
            if out is not None:
                recording = not recording
                status = "ON" if recording else "OFF"
                logger.info(f"Recording: {status}")
        
        # === Log stats every 100 frames ===
        if frame_count % 100 == 0:
            if avg_latency:
                avg_lat = sum(avg_latency) / len(avg_latency)
                logger.info(f"📊 Frame {frame_count} | FPS: {fps:.1f} | "
                           f"Avg Latency: {avg_lat:.0f}ms")
    
    # =====================
    # Cleanup
    # =====================
    logger.info("=" * 70)
    logger.info("✓ Cleaning up...")
    
    cap.release()
    if out:
        out.release()
    cv2.destroyAllWindows()
    
    logger.info(f"✓ Total frames: {frame_count}")
    logger.info(f"✓ Average FPS: {frame_count / (time.time() - start_time):.1f}")
    
    if avg_latency:
        avg_lat = sum(avg_latency) / len(avg_latency)
        logger.info(f"✓ Average Latency: {avg_lat:.0f}ms")


if __name__ == '__main__':
    main()
