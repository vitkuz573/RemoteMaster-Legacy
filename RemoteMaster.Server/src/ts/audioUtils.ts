let audioContext: AudioContext | null = null;

/**
 * Initializes the AudioContext.
 */
export function initAudioContext(): void {
    if (!audioContext) {
        audioContext = new AudioContext();
        audioContext.resume();
    }
}

/**
 * Converts raw PCM data (32-bit, little-endian) to Float32Array.
 * @param audioData The raw PCM audio data as a Uint8Array.
 * @returns A Float32Array representing the audio samples.
 */
function convertPCMToFloat32(audioData: Uint8Array): Float32Array {
    const samples = audioData.length / 4; // 32-bit audio (4 bytes per sample)
    const float32Data = new Float32Array(samples);
    const dataView = new DataView(audioData.buffer);

    for (let i = 0; i < samples; i++) {
        const int32 = dataView.getInt32(i * 4, true); // little endian
        float32Data[i] = int32 / 2147483648; // Normalize to [-1, 1] for 32-bit PCM
    }

    return float32Data;
}

/**
 * Plays an audio chunk in raw PCM format.
 * @param audioData The raw PCM audio data as a Uint8Array.
 */
export async function playAudioChunk(audioData: Uint8Array): Promise<void> {
    initAudioContext();

    if (!audioContext) {
        console.error("AudioContext is not initialized.");
        return;
    }

    if (audioData.length === 0) {
        console.error("Received empty audio data.");
        return;
    }

    try {
        const sampleRate = 48000; // 48000 Hz as per your C# capture settings
        const numberOfChannels = 2; // Stereo

        // Convert PCM to Float32
        const float32Data = convertPCMToFloat32(audioData);

        if (float32Data.length === 0) {
            console.error("No valid audio data to process.");
            return;
        }

        // Calculate the number of samples per channel
        const samplesPerChannel = Math.floor(float32Data.length / numberOfChannels);

        // Create an AudioBuffer
        const audioBuffer = audioContext.createBuffer(
            numberOfChannels,
            samplesPerChannel,
            sampleRate
        );

        // Assign data to each channel
        for (let channel = 0; channel < numberOfChannels; channel++) {
            const channelData = audioBuffer.getChannelData(channel);
            for (let i = 0; i < samplesPerChannel; i++) {
                channelData[i] = float32Data[i * numberOfChannels + channel];
            }
        }

        // Play the buffer
        const source = audioContext.createBufferSource();
        source.buffer = audioBuffer;
        source.connect(audioContext.destination);
        source.start();
    } catch (error) {
        console.error("Error decoding and playing audio chunk:", error);
    }
}
