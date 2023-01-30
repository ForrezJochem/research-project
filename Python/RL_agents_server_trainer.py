import json
import pickle
import socket
import numpy as np
import tkinter as tk
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers


port_python_client = 5001
port_gta_client = 5000

gamma = 0.99 
epsilon = 0.99
epsilon_min = 0.01
epsilon_max = 1.0  
epsilon_interval = epsilon_max - epsilon_min 
batch_size = 4
max_steps_per_episode = 100000
height, width = 128, 227
num_actions = 5


def create_q_model():
    input = layers.Input(shape=(height, width, 3))

    # convolutions on the frames on the screen
    layer1 = layers.Conv2D(64, 8, strides=4, activation="relu")(input)
    layer2 = layers.Conv2D(128, 4, strides=2, activation="relu")(layer1)
    layer3 = layers.Conv2D(256, 4, strides=1, activation="relu")(layer2)
    layer4 = layers.Flatten()(layer3)
    layer5 = layers.Dense(128, activation="relu")(layer4)
    action = layers.Dense(num_actions, activation="linear")(layer5)
    return keras.Model(inputs=input, outputs=action)

model = create_q_model()
model_target = create_q_model()
model.summary()

# trys to load the model if it exists
try:
    model.load_weights("model.h5")
    model_target.load_weights("model.h5")
except:
    print("model not found")


print("attempting to connect to python client")
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind(("0.0.0.0", port_python_client))
s.listen()
clientsocketpython, address = s.accept()
print("connected to python client")
print("attempting to connect to gta client")
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
s.bind(("0.0.0.0", port_gta_client))
s.listen()
clientsocketgta, address = s.accept()
print("connected to gta client")


def get_gta_data():
    while True:
        try:
            clientsocketgta.send(bytes("true", "utf-8"))
            data = clientsocketgta.recv(1024)
            data = data.decode("utf-8")
            data = json.loads(data)
            break
        except:
            pass
    return data

def send_python_data(data):
    try:
        data = json.dumps(int(data))
        clientsocketpython.send(bytes(data, "utf-8"))
        data = clientsocketpython.recv(1024)
        data = data.decode("utf-8")
        if (data == "true"):
            return
    except:
        print("error in json dump")


def get_gta_image():
    while True:
        try:
            clientsocketpython.send(bytes("true", "utf-8"))
            data = clientsocketpython.recv(697540)
            data = pickle.loads(data)
            break
        except:
            pass
    return data

def calc_reward(data):
    reward = 0
    reward += data["Distance"]
    if (abs(data["Speed"]) > 1):
        reward += 2
    else:
        reward -= 1
    reward -= data["Damage"]
    if (data["Success"]):
        reward += 1000
    return reward

window = tk.Tk()
window.title("RL Agent Server")
window.update()


# optimizer adam with amsgrad and learning rate of 0.00025
optimizer = keras.optimizers.Adam(learning_rate=0.00025, amsgrad=True)


action_history = []
state_history = []
state_next_history = []
rewards_history = []
done_history = []
episode_reward_history = []
running_reward = 0
episode_count = 0
frame_count = 0
# Number of frames to take random action and observe output
epsilon_random_frames = 50000
# Number of frames for exploration
epsilon_greedy_frames = 1000000.0
max_memory_length = 2000
update_after_actions = 10
update_target_network = 1000
# Using huber loss for stability
loss_function = keras.losses.Huber()
loss = None

while True:
    state = get_gta_image()
    episode_reward = 0
    
    for timestep in range(1, max_steps_per_episode):
        frame_count += 1
        # check if we should take a random action or the best action
        if frame_count < epsilon_random_frames or epsilon > np.random.rand(1)[0]:
            # take a random action
            action = np.random.choice(num_actions)
            action_tk_label = "random"
        else:
            # take the best action based on the model
            state_tensor = tf.convert_to_tensor(state)
            state_tensor = tf.expand_dims(state_tensor, 0)
            action_probs = model(state_tensor, training=False)
            action = tf.argmax(action_probs[0]).numpy()
            action_tk_label = "best"

        # reduce epsilon over time to encourage the agent to explore
        epsilon -= epsilon_interval / epsilon_greedy_frames
        epsilon = max(epsilon, epsilon_min)
        
        # get the next state
        state_next = get_gta_image()
        # get the data from gta
        data = get_gta_data()
        done = data["HardReset"]
        # calculate the reward
        reward = calc_reward(data)
        # send the action to python client
        send_python_data(action)
        state_next = np.array(state_next)
        episode_reward += reward
        # save the data to the replay buffer
        action_history.append(action)
        state_history.append(state)
        state_next_history.append(state_next)
        done_history.append(done)
        rewards_history.append(reward)
        state = state_next

        # update the model
        if frame_count % update_after_actions == 0 and len(done_history) > batch_size:
            # get indices of samples for replay buffers
            indices = np.random.choice(range(len(done_history)), size=batch_size)

            # using list comprehension to sample from replay buffer
            state_sample = np.array([state_history[i] for i in indices])
            state_next_sample = np.array([state_next_history[i] for i in indices])
            rewards_sample = [rewards_history[i] for i in indices]
            action_sample = [action_history[i] for i in indices]
            done_sample = tf.convert_to_tensor([float(done_history[i]) for i in indices])


            future_rewards = model_target.predict(state_next_sample)
            # Q value = reward + discount factor * expected future reward
            updated_q_values = rewards_sample + gamma * tf.reduce_max(
                future_rewards, axis=1)

            updated_q_values = updated_q_values * (1 - done_sample) - done_sample

            # Create a mask so we only calculate loss on the updated Q-values
            masks = tf.one_hot(action_sample, num_actions)

            with tf.GradientTape() as tape:
                # Train the model on the states and updated Q-values
                q_values = model(state_sample)

                # Apply the masks to the Q-values to get the Q-value for action taken
                q_action = tf.reduce_sum(tf.multiply(q_values, masks), axis=1)
                # Calculate loss between new Q-value and old Q-value
                loss = loss_function(updated_q_values, q_action)

            # Backpropagation
            grads = tape.gradient(loss, model.trainable_variables)
            optimizer.apply_gradients(zip(grads, model.trainable_variables))

        if frame_count % update_target_network == 0:
            # update the the target network with new weights
            model_target.set_weights(model.get_weights())
            model_target.save_weights('model.h5')
            clientsocketgta.send(bytes("reset", "utf-8"))
            print ("running reward: " + str(running_reward) + " at episode " + str(episode_count) + ", frame count " + str(frame_count) + " epsilon: " + str(epsilon))
            done = True

        # Limit the state and reward history
        if len(rewards_history) > max_memory_length:
            del rewards_history[:1]
            del state_history[:1]
            del state_next_history[:1]
            del action_history[:1]
            del done_history[:1]

        try:
            epsilonTk.destroy()
            rewardTk.destroy()
            actionTk.destroy()
            gtaDataTk.destroy()
            lossTk.destroy()
        except:
            pass


        epsilonTk = tk.Label(window, text="epsilon: " + str(epsilon))
        lossTk = tk.Label(window, text="loss: " + str(loss))
        actionTk = tk.Label(window, text="action: " + str(action_tk_label))
        gtaDataTk = tk.Label(window, text="GTA Data: " + str(data))
        rewardTk = tk.Label(window, text="Episode reward: " + str(episode_reward))

        
        epsilonTk.pack()
        actionTk.pack()
        rewardTk.pack()
        gtaDataTk.pack()
        lossTk.pack()
        
        window.update()

        if done:
            break

    episode_reward_history.append(episode_reward)
    if len(episode_reward_history) > 100:
        del episode_reward_history[:1]
    running_reward = np.mean(episode_reward_history)

    episode_count += 1

    if running_reward > 300:  # Condition to consider the task solved
        print("Solved at episode {}!".format(episode_count))
        break