Neutralino.init();

Neutralino.events.on("windowClose", () => {
    Neutralino.app.exit();
});

let connection;
const currentUser = 'User';
let replyToMessageId = null;
let typingTimeout;
let selectedFiles = [];

async function getIpAddress() {
    try {
        const data = await Neutralino.filesystem.readFile('C:\\Program Files\\RemoteMaster\\Host\\RemoteMaster.Host.json');
        const config = JSON.parse(data);
        return config.host.ipAddress;
    } catch (err) {
        console.error('Failed to read IP address from configuration file:', err);
        updateStatus(`Failed to read IP address: ${err.message || err}`);
        return null;
    }
}

function formatTimestamp(timestamp) {
    const date = new Date(timestamp);
    return date.toLocaleString();
}

async function connectToSignalR() {
    const ipAddress = await getIpAddress();
    if (!ipAddress) {
        updateStatus('Failed to read IP address');
        return;
    }

    connection = new signalR.HubConnectionBuilder()
        .withUrl(`https://${ipAddress}:5001/hubs/chat`)
        .build();

    connection.on('ReceiveMessage', (message) => {
        addMessageToUI(message);
    });

    connection.on('MessageDeleted', (messageId) => {
        const messageElement = document.getElementById(`message-${messageId}`);
        if (messageElement) {
            messageElement.remove();
        }
    });

    connection.on('UserTyping', (user) => {
        document.getElementById('typing-indicator').innerText = `${user} is typing...`;
    });

    connection.on('UserStopTyping', (user) => {
        document.getElementById('typing-indicator').innerText = '';
    });

    try {
        await connection.start();
        updateStatus('Connected to SignalR', true);
    } catch (err) {
        updateStatus(`Failed to connect: ${err.message || err}`, false);
    }

    connection.onclose(async () => {
        updateStatus('Disconnected. Reconnecting...', false);
        try {
            await connection.start();
            updateStatus('Reconnected to SignalR', true);
        } catch (err) {
            updateStatus(`Failed to reconnect: ${err.message || err}`, false);
        }
    });
}

function addMessageToUI(message) {
    const placeholder = document.getElementById('placeholder');
    if (placeholder) {
        placeholder.style.display = 'none';
    }

    const messageContainer = document.createElement('div');
    messageContainer.classList.add('message-container', 'flex', 'mb-4');

    const messageElement = document.createElement('div');
    messageElement.id = `message-${message.id}`;
    messageElement.classList.add('message', 'rounded-lg', 'p-4', 'relative', 'flex', 'flex-col', 'max-w-full', 'break-words', 'shadow', 'bg-white', 'w-1/2');

    const header = document.createElement('div');
    header.classList.add('message-header', 'flex', 'justify-between', 'items-center', 'mb-2');

    const userInfo = document.createElement('div');
    userInfo.classList.add('flex', 'items-center');

    const username = document.createElement('span');
    username.classList.add('message-username', 'font-bold', 'mr-2');
    username.textContent = message.user;

    const time = document.createElement('span');
    time.classList.add('message-time', 'text-xs', 'text-gray-500');
    time.textContent = formatTimestamp(message.timestamp);

    userInfo.appendChild(username);
    userInfo.appendChild(time);

    const actionButtons = document.createElement('div');
    actionButtons.classList.add('flex', 'items-center');

    const replyButton = document.createElement('button');
    replyButton.classList.add('reply-button', 'bg-blue-500', 'text-white', 'rounded-full', 'w-6', 'h-6', 'flex', 'items-center', 'justify-center', 'cursor-pointer', 'mr-2');
    replyButton.innerHTML = '<span class="material-icons">reply</span>';
    replyButton.onclick = () => setReplyToMessage(message.id);

    const deleteButton = document.createElement('button');
    deleteButton.classList.add('delete-button', 'bg-red-500', 'text-white', 'rounded-full', 'w-6', 'h-6', 'flex', 'items-center', 'justify-center', 'cursor-pointer');
    deleteButton.innerHTML = '<span class="material-icons">delete</span>';
    deleteButton.onclick = () => deleteMessage(message.id);

    actionButtons.appendChild(replyButton);
    if (message.user === currentUser) {
        actionButtons.appendChild(deleteButton);
    }

    header.appendChild(userInfo);
    header.appendChild(actionButtons);

    const text = document.createElement('div');
    text.classList.add('message-text', 'word-wrap', 'break-word', 'mt-2');
    text.textContent = message.message;

    if (message.replyToId) {
        const repliedMessage = document.getElementById(`message-${message.replyToId}`);
        if (repliedMessage) {
            const repliedText = repliedMessage.querySelector('.message-text').textContent;
            const replyPreview = document.createElement('div');
            replyPreview.classList.add('reply-preview', 'text-xs', 'text-gray-500', 'mb-2', 'pl-2', 'border-l', 'border-gray-400');
            replyPreview.innerHTML = `Replying to: <a href="#" onclick="highlightMessage('${message.replyToId}'); return false;" class="text-blue-500 hover:underline">${repliedText.substring(0, 30)}...</a>`;
            messageElement.appendChild(replyPreview);
        }
    }

    messageElement.appendChild(header);

    if (message.attachments && message.attachments.length > 0) {
        message.attachments.forEach(attachment => {
            if (attachment.mimeType.startsWith('image/')) {
                const image = document.createElement('img');
                image.src = `data:${attachment.mimeType};base64,${attachment.data}`;
                image.classList.add('mt-2', 'rounded', 'max-w-xs');
                messageElement.appendChild(image);
            } else {
                const fileLink = document.createElement('a');
                fileLink.href = `data:${attachment.mimeType};base64,${attachment.data}`;
                fileLink.download = attachment.fileName;
                fileLink.textContent = `Download ${attachment.fileName}`;
                fileLink.classList.add('mt-2', 'text-blue-500', 'hover:underline');
                messageElement.appendChild(fileLink);
            }
        });
    }

    messageElement.appendChild(text);

    if (message.user === currentUser) {
        messageElement.classList.add('bg-green-100', 'self-end', 'rounded-tl-none');
        messageContainer.style.justifyContent = 'flex-end';
    } else {
        messageElement.classList.add('bg-gray-100', 'self-start', 'rounded-tr-none');
        messageContainer.style.justifyContent = 'flex-start';
    }

    messageContainer.appendChild(messageElement);
    const messagesContainer = document.getElementById('messages');
    messagesContainer.insertBefore(messageContainer, messagesContainer.firstChild);
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function sendMessage() {
    const messageInput = document.getElementById('messageInput');
    const message = messageInput.value;
    if (message || selectedFiles.length > 0) {
        const messageDto = {
            user: currentUser,
            message: message,
            replyToId: replyToMessageId,
            attachments: selectedFiles.map(file => ({
                fileName: file.fileName,
                data: Array.from(new Uint8Array(file.data)),
                mimeType: file.mimeType
            }))
        };
        connection.invoke('SendMessage', messageDto).catch(err => console.error(err));
        messageInput.value = '';
        replyToMessageId = null;
        document.getElementById('reply-text').textContent = '';
        document.getElementById('reply-indicator').style.display = 'none';
        removeFiles();
    }
}

function handleFileInput(files) {
    selectedFiles = [];
    for (const file of files) {
        const reader = new FileReader();
        reader.onload = (e) => {
            const arrayBuffer = e.target.result;
            selectedFiles.push({
                fileName: file.name,
                data: arrayBuffer,
                mimeType: file.type
            });
            showFilePreview(file);
        };
        reader.readAsArrayBuffer(file);
    }
}

function showFilePreview(file) {
    const preview = document.getElementById('file-preview');
    const filePreviewElement = document.createElement('div');
    filePreviewElement.classList.add('file-preview', 'mt-2', 'flex', 'items-center', 'justify-between', 'border', 'border-gray-300', 'rounded', 'p-2');

    const fileName = document.createElement('span');
    fileName.textContent = file.name;
    filePreviewElement.appendChild(fileName);

    const removeButton = document.createElement('button');
    removeButton.classList.add('ml-2', 'text-red-500', 'hover:text-red-700');
    removeButton.innerHTML = '<span class="material-icons">close</span>';
    removeButton.onclick = () => {
        selectedFiles = selectedFiles.filter(f => f.fileName !== file.name);
        filePreviewElement.remove();
    };
    filePreviewElement.appendChild(removeButton);

    preview.appendChild(filePreviewElement);
    preview.style.display = 'block';
}

function removeFiles() {
    selectedFiles = [];
    const preview = document.getElementById('file-preview');
    preview.style.display = 'none';
    preview.innerHTML = '';
    document.getElementById('fileInput').value = '';
}

function deleteMessage(messageId) {
    if (connection) {
        connection.invoke('DeleteMessage', messageId, currentUser).catch(err => console.error(err));
    }
}

function setReplyToMessage(messageId) {
    replyToMessageId = messageId;
    const repliedMessage = document.getElementById(`message-${messageId}`);
    if (repliedMessage) {
        const repliedText = repliedMessage.querySelector('.message-text').textContent;
        const replyIndicator = document.getElementById('reply-text');
        replyIndicator.textContent = `Replying to: ${repliedText.substring(0, 30)}...`;
        document.getElementById('reply-indicator').style.display = 'flex';
    }
}

function cancelReply() {
    replyToMessageId = null;
    document.getElementById('reply-text').textContent = '';
    document.getElementById('reply-indicator').style.display = 'none';
}

function notifyTyping() {
    connection.invoke('Typing', currentUser).catch(err => console.error(err));
    clearTimeout(typingTimeout);
    typingTimeout = setTimeout(() => {
        connection.invoke('StopTyping', currentUser).catch(err => console.error(err));
    }, 1000);
}

function highlightMessage(messageId) {
    const messageElement = document.getElementById(`message-${messageId}`);
    if (messageElement) {
        messageElement.classList.add('bg-yellow-300');
        setTimeout(() => {
            messageElement.classList.remove('bg-yellow-300');
        }, 2000);
        messageElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }
}

function checkForSend(event) {
    if (event.ctrlKey && event.key === 'Enter') {
        sendMessage();
    }
}

function updateStatus(message, isConnected) {
    const statusElement = document.getElementById('status');
    const indicatorElement = document.getElementById('connection-indicator');

    statusElement.innerText = message;
    if (isConnected) {
        indicatorElement.classList.replace('bg-red-500', 'bg-green-500');
    } else {
        indicatorElement.classList.replace('bg-green-500', 'bg-red-500');
    }
}

document.getElementById('messageInput').addEventListener('input', notifyTyping);
document.getElementById('messageInput').addEventListener('keydown', checkForSend);

document.addEventListener('paste', (event) => {
    const items = (event.clipboardData || event.originalEvent.clipboardData).items;
    for (const item of items) {
        if (item.kind === 'file') {
            const blob = item.getAsFile();
            handleFileInput([blob]);
        }
    }
});

document.getElementById('fileInput').addEventListener('change', (event) => {
    handleFileInput(event.target.files);
});

connectToSignalR();
