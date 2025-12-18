"""
gRPC Python kodunu proto dosyasından oluşturur
Kullanım: python generate_grpc.py
"""

import subprocess
import sys
import os

def generate_grpc_code():
    """Proto dosyasından Python gRPC kodu üret"""
    
    proto_file = "proto/prediction.proto"
    
    if not os.path.exists(proto_file):
        print(f"✗ Proto dosyası bulunamadı: {proto_file}")
        return False
    
    print("gRPC Python kodu üretiliyor...")
    
    cmd = [
        sys.executable,
        "-m",
        "grpc_tools.protoc",
        f"-I.",
        f"--python_out=.",
        f"--grpc_python_out=.",
        proto_file
    ]
    
    try:
        result = subprocess.run(cmd, check=True, capture_output=True, text=True)
        print("✓ gRPC kodu başarıyla üretildi")
        print(f"  - proto/prediction_pb2.py")
        print(f"  - proto/prediction_pb2_grpc.py")
        return True
    except subprocess.CalledProcessError as e:
        print(f"✗ Hata: {e}")
        print(f"Stderr: {e.stderr}")
        return False

if __name__ == "__main__":
    success = generate_grpc_code()
    sys.exit(0 if success else 1)

