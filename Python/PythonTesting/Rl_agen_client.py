import time
import dxcam
import socket
import pickle
import win32gui
import skimage.transform as st
from pynput.keyboard import Key, Controller

# ip off the server (the machine that runs the RL_agents_server.py file)
ip = "192.168.0.136"
# port off the server (the machine that runs the RL_agents_server.py file)
port = 5001
# the size of the image that will be sent to the server
height, width = 128, 227

# the code below is used to get the window of the game
hwnd = win32gui.FindWindow(None, "Grand Theft Auto V")
window = win32gui.GetWindowRect(hwnd)
window = (window[0] + 8, window[1] + 31, window[2] - 8, window[3] - 8)
camera = dxcam.create(output_color="RGB")

# the code below is used to mimic the keyboard
keyboard = Controller()

# the code below is used to connect to the server
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.connect((ip, port ))


# the code below is used to get the image of the game
def get_screenshot():
    frame = camera.grab(region=(window))
    while frame is None:
        frame = camera.grab(region=(window))
    # resize the image
    frame = st.resize(frame, (height, width))
    return frame


def do_action(action):
    try:
        if (type(action) != int):
            action = int(action)
        if (action == 0):
            keyboard.press('z')
            time.sleep(0.2)
            keyboard.release('z')
        elif (action == 1):
            keyboard.press('s')
            time.sleep(0.2)
            keyboard.release('s')
        elif (action == 2):
            keyboard.press('q')
            time.sleep(0.2)
            keyboard.release('q')
        elif (action == 3):
            keyboard.press('d')
            time.sleep(0.2)
            keyboard.release('d')
        elif (action == 4):
            keyboard.press(Key.space)
            time.sleep(0.2)
            keyboard.release(Key.space)
        s.send("true".encode("utf-8"))
    except:
        pass
   
# the code below is used to send the image to the server and get the action from te server
while True:
    data = s.recv(1024)
    data = data.decode("utf-8")
    if data == "true":
        frame = get_screenshot()
        frame = pickle.dumps(frame)
        s.send(frame)
    else:
        do_action(data)