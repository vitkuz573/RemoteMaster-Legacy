let audioContext: AudioContext | null = null;

/**
 * Initializes the AudioContext if it is not already initialized.
 * If the AudioContext is suspended, attempts to resume it.
 */
export function initAudioContext(): void {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    if (audioContext.state === 'suspended') {
        audioContext.resume().catch(err => {
            console.warn("AudioContext resume attempt failed:", err);
        });
    }
}

/**
 * Attempts to resume the AudioContext if it's currently suspended.
 * This should be called after a user gesture (e.g. from a button click).
 */
export function resumeAudioContext(): void {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    if (audioContext.state === 'suspended') {
        audioContext.resume().then(() => {
            console.log("AudioContext resumed by user action.");
        }).catch(err => {
            console.error("Failed to resume AudioContext:", err);
        });
    } else {
        console.log("AudioContext is already running or closed.");
    }
}

/**
 * Decodes a base64 string to a Uint8Array.
 * @param base64 The base64 encoded string.
 * @returns A Uint8Array of the decoded bytes.
 */
function base64ToUint8Array(base64: string): Uint8Array {
    const binaryString = atob(base64);
    const length = binaryString.length;
    const bytes = new Uint8Array(length);
    for (let i = 0; i < length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes;
}

/**
 * Plays an audio chunk that is provided as a base64 string (since C# byte[] is passed as Base64 to JS).
 * @param audioDataBase64 The raw PCM audio data as a Base64 string.
 */
export async function playAudioChunk(audioDataBase64: string): Promise<void> {
    initAudioContext();

    if (!audioContext) {
        console.error("AudioContext is not initialized.");
        return;
    }

    const audioData = base64ToUint8Array(audioDataBase64);

    if (audioData.length === 0) {
        console.error("Received empty audio data.");
        return;
    }

    console.log("Received audioData length:", audioData.length);

    const count = Math.min(20, audioData.length);
    const hexBytes: string[] = [];
    const decBytes: number[] = [];
    for (let i = 0; i < count; i++) {
        const val = audioData[i];
        hexBytes.push(val.toString(16).padStart(2, '0'));
        decBytes.push(val);
    }

    console.log(`Raw first ${count} bytes (hex):`, hexBytes.join(' '));
    console.log(`Raw first ${count} bytes (decimal):`, decBytes);

    try {
        const sampleRate = 48000;
        const numberOfChannels = 2;

        const samples = audioData.length / 4;
        const float32Data = new Float32Array(samples);
        const dataView = new DataView(audioData.buffer);

        for (let i = 0; i < samples; i++) {
            float32Data[i] = dataView.getFloat32(i * 4, true);
        }

        if (float32Data.length === 0) {
            console.error("No valid audio data to process.");
            return;
        }

        console.log("First 10 float samples:", float32Data.slice(0, 10));

        const samplesPerChannel = Math.floor(float32Data.length / numberOfChannels);
        const audioBuffer = audioContext.createBuffer(numberOfChannels, samplesPerChannel, sampleRate);

        for (let channel = 0; channel < numberOfChannels; channel++) {
            const channelData = audioBuffer.getChannelData(channel);
            for (let i = 0; i < samplesPerChannel; i++) {
                channelData[i] = float32Data[i * numberOfChannels + channel];
            }
        }

        const source = audioContext.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(audioContext.destination);
        source.start();
    } catch (error) {
        console.error("Error decoding and playing audio chunk:", error);
    }
}
