"""
Smart Parking WebSocket Server Example
Backend để test WebSocket Client

Installation:
    pip install python-socketio python-engineio aiohttp

Usage:
    python websocket_server_example.py

Thì client sẽ kết nối tới ws://localhost:5001 và có latency ~50ms thay vì ~200-500ms HTTP
"""

import asyncio
import logging
import time
import json
import base64
from datetime import datetime
from aiohttp import web
from socketio import AsyncServer, ASGIApp

# =====================
# Setup Logging
# =====================
logging.basicConfig(
    level=logging.INFO,
    format='[%(asctime)s] [%(name)s] [%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

# =====================
# Socket.IO Setup
# =====================
sio = AsyncServer(
    async_mode='aiohttp',
    cors_allowed_origins='*',
    ping_interval=25,
    ping_timeout=60
)

app = web.Application()
sio.attach(app)

# Statistics
stats = {
    'total_connections': 0,
    'active_connections': 0,
    'plates_received': 0,
    'plates_processed': 0
}

# =====================
# Socket.IO Events
# =====================

@sio.event
async def connect(sid, environ=None):
    """Xử lý client kết nối."""
    stats['total_connections'] += 1
    stats['active_connections'] += 1
    logger.info(f"✓ [Client {sid}] Connected | Total: {stats['total_connections']}")


@sio.event
async def disconnect(sid):
    """Xử lý client ngắt kết nối."""
    stats['active_connections'] -= 1
    logger.info(f"✗ [Client {sid}] Disconnected | Active: {stats['active_connections']}")


@sio.event
async def plate_detected(sid, data):
    """
    Nhận sự kiện plate_detected từ client.
    
    Data structure:
    {
        'plate_number': '51K-123',
        'station_id': 'STATION_01',
        'confidence': 0.95,
        'timestamp': '2026-04-04T12:34:56',
        'image_b64': 'base64_encoded_image'
    }
    """
    
    stats['plates_received'] += 1
    
    try:
        plate_number = data.get('plate_number', 'unknown')
        station_id = data.get('station_id', 'unknown')
        confidence = data.get('confidence', 0.0)
        image_b64 = data.get('image_b64', '')
        
        # Log nhận được
        logger.info(f"📸 Plate detected: {plate_number} | "
                   f"Confidence: {confidence:.2f} | "
                   f"Station: {station_id} | "
                   f"Image size: {len(image_b64)} bytes")
        
        # === Xử lý dữ liệu ===
        # TODO: Gọi database, kiểm tra parking status, etc.
        
        # Giả lập xử lý (sleep 50ms)
        await asyncio.sleep(0.05)
        
        stats['plates_processed'] += 1
        
        # === Response ===
        response = {
            'status': 'success',
            'message': f'Plate {plate_number} processed',
            'transactionId': f'TXN_{plate_number}_{int(time.time())}',
            'timestamp': datetime.now().isoformat(),
            'server_latency_ms': 50
        }
        
        logger.info(f"✓ Response sent for {plate_number}")
        
        return response
    
    except Exception as e:
        logger.error(f"✗ Error processing plate: {str(e)}")
        return {
            'status': 'error',
            'message': str(e)
        }


@sio.event
async def ping(sid, data):
    """Health check endpoint."""
    return {'status': 'pong', 'timestamp': datetime.now().isoformat()}


# =====================
# HTTP Routes (Health Check)
# =====================

async def health_handler(request):
    """HTTP health check."""
    return web.json_response({
        'status': 'healthy',
        'websocket': True,
        'timestamp': datetime.now().isoformat(),
        'stats': stats
    })


async def stats_handler(request):
    """Xem thống kê."""
    return web.json_response({
        'total_connections': stats['total_connections'],
        'active_connections': stats['active_connections'],
        'plates_received': stats['plates_received'],
        'plates_processed': stats['plates_processed']
    })


# =====================
# Routes Setup
# =====================

app.router.add_get('/health', health_handler)
app.router.add_get('/api/parking/health', health_handler)  # Fallback route
app.router.add_get('/api/parking/stats', stats_handler)


# =====================
# Startup/Shutdown
# =====================

async def startup(app):
    logger.info("=" * 50)
    logger.info("🚀 WebSocket Server Starting...")
    logger.info("=" * 50)
    logger.info(f"WS URL: ws://localhost:5001")
    logger.info(f"Health Check: http://localhost:5001/health")
    logger.info(f"Stats: http://localhost:5001/api/parking/stats")
    logger.info("=" * 50)


if __name__ == '__main__':
    import asyncio
    
    # Add startup event
    app.on_startup.append(startup)
    
    # Run server
    try:
        logger.info("Starting WebSocket server on ws://localhost:5001")
        web.run_app(app, host='0.0.0.0', port=5001)
    except KeyboardInterrupt:
        logger.info("\n✗ Server shutdown")
