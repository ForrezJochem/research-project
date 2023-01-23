from http import client
import json
import socket
import time

s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind(("127.0.0.1", 5000))
s.listen(0)
clientsocket, address = s.accept()
time.sleep(3)
clientsocket.send(bytes("true", "utf-8"))
while True:
    data = clientsocket.recv(1024)
    data = data.decode("utf-8")
    data = json.loads(data)
    #print(data["Distance"])
    print(data)
    time.sleep(1)
    clientsocket.send(bytes("true", "utf-8"))