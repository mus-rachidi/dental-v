import hashlib, base64, sys

SECRET = "ClinicManager-License-Secret-2026-XK9#mP2$vL"

def generate_key(machine_id: str) -> str:
    machine_id = machine_id.strip().upper()
    payload = f"{machine_id}:{SECRET}"
    h = hashlib.sha256(payload.encode()).digest()
    encoded = base64.b64encode(h).decode()
    encoded = encoded.replace("+", "X").replace("/", "Y").replace("=", "")
    encoded = encoded[:25].upper()
    return f"{encoded[0:5]}-{encoded[5:10]}-{encoded[10:15]}-{encoded[15:20]}-{encoded[20:25]}"

if __name__ == "__main__":
    if len(sys.argv) > 1:
        mid = sys.argv[1]
    else:
        mid = input("Enter Machine ID: ")
    print(f"\nMachine ID:  {mid.strip().upper()}")
    print(f"License Key: {generate_key(mid)}")
