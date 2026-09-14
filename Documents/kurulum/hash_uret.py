"""
ASP.NET Core Identity v3 formatında parola hash'i üretir (PasswordHasher<User> ile uyumlu).
Kullanım:  python hash_uret.py "GucluParola123!"
Çıktıyı 02-roller-ve-admin.sql içindeki <HASH_BURAYA> yerine yapıştırın.
"""
import base64, hashlib, os, struct, sys

def identity_v3_hash(password: str, iterations: int = 100_000) -> str:
    salt = os.urandom(16)
    subkey = hashlib.pbkdf2_hmac("sha512", password.encode("utf-8"), salt, iterations, dklen=32)
    header = bytes([0x01]) + struct.pack(">III", 2, iterations, len(salt))  # 0x01 = v3, 2 = HMACSHA512
    return base64.b64encode(header + salt + subkey).decode()

if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit('Kullanım: python hash_uret.py "Parola"')
    print(identity_v3_hash(sys.argv[1]))
