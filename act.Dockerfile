FROM ghcr.io/catthehacker/ubuntu:full-24.04

# AppArmor doesn't work well in containers and breaks unityhub's post-install script
RUN sudo apt-get purge apparmor

# Pre-install unityhub for faster iteration
RUN wget -qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor | sudo tee /usr/share/keyrings/Unity_Technologies_ApS.gpg > /dev/null && \
    sudo sh -c 'echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" > /etc/apt/sources.list.d/unityhub.list' && \
    sudo apt-get update && sudo apt-get install unityhub

# unity-setup action expects a unity-hub wrapper script, urgh
RUN sudo ln -s /usr/bin/unityhub /usr/bin/unity-hub

# sudo podman build -f ./act.Dockerfile -t js6pak/ubuntu:full-24.04 --omit-history
