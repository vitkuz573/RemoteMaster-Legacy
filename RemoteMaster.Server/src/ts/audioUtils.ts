let audioContext: AudioContext | null = null;
let audioAccumulator: Float32Array[] = [];
let playTimer: number | null = null;

const CHUNK_ACCUMULATION_COUNT = 5;    // Number of chunks to accumulate before playing
const SAMPLE_RATE = 48000;             // Your audio sample rate
const NUMBER_OF_CHANNELS = 2;          // Your number of channels (e.g., 2 for stereo)
const PLAY_INTERVAL_MS = 500;          // Play accumulated chunks every 500ms (adjust as needed)

/**
 * Initializes the AudioContext if not already initialized.
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
 * Should be called after a user gesture (e.g., a button click).
 */
export function resumeAudioContext(): void {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    if (audioContext.state === 'suspended') {
        audioContext.resume().catch(err => {
            console.error("Failed to resume AudioContext:", err);
        });
    }
}

/**
 * Decodes a base64 string into a Uint8Array of raw bytes.
 * @param base64 The base64 encoded string.
 * @returns A Uint8Array representing the decoded bytes.
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
 * Processes a single audio chunk (base64-encoded float32 PCM),
 * decodes it, converts it to Float32Array samples, and adds it
 * to the accumulator for later playback.
 * @param audioDataBase64 The raw PCM audio data in Base64 format.
 */
export async function playAudioChunk(audioDataBase64: string): Promise<void> {
    initAudioContext();

    if (!audioContext) {
        console.error("AudioContext is not initialized.");
        return;
    }

    // Decode base64 to bytes
    const audioData = base64ToUint8Array(audioDataBase64);
    if (audioData.length === 0) {
        // Empty data, ignore
        return;
    }

    // Interpret the bytes as float32 samples
    const samples = audioData.length / 4;
    const float32Data = new Float32Array(samples);
    const dataView = new DataView(audioData.buffer);

    for (let i = 0; i < samples; i++) {
        float32Data[i] = dataView.getFloat32(i * 4, true);
    }

    // Accumulate the decoded Float32Array
    audioAccumulator.push(float32Data);

    // Set or reset a timer to play accumulated chunks after a delay
    if (playTimer !== null) {
        clearTimeout(playTimer);
    }
    playTimer = window.setTimeout(playAccumulatedChunks, PLAY_INTERVAL_MS);

    // If we've accumulated enough chunks, we can decide to play right now
    if (audioAccumulator.length >= CHUNK_ACCUMULATION_COUNT) {
        playAccumulatedChunks();
    }
}

/**
 * Combines all accumulated Float32Arrays into one larger Float32Array,
 * creates an AudioBuffer, and plays it back. This reduces pops and clicks
 * compared to playing tiny chunks one by one.
 */
function playAccumulatedChunks(): void {
    if (!audioContext || audioAccumulator.length === 0) {
        return;
    }

    // Combine all accumulated arrays into one
    let totalLength = 0;
    for (const chunk of audioAccumulator) {
        totalLength += chunk.length;
    }

    const combined = new Float32Array(totalLength);
    let offset = 0;
    for (const chunk of audioAccumulator) {
        combined.set(chunk, offset);
        offset += chunk.length;
    }

    // Clear the accumulator
    audioAccumulator = [];

    // Create AudioBuffer from combined data
    const samplesPerChannel = Math.floor(combined.length / NUMBER_OF_CHANNELS);
    const audioBuffer = audioContext.createBuffer(NUMBER_OF_CHANNELS, samplesPerChannel, SAMPLE_RATE);

    // Distribute samples into channels
    for (let ch = 0; ch < NUMBER_OF_CHANNELS; ch++) {
        const channelData = audioBuffer.getChannelData(ch);
        for (let i = 0; i < samplesPerChannel; i++) {
            channelData[i] = combined[i * NUMBER_OF_CHANNELS + ch];
        }
    }

    // Play the combined buffer
    const source = audioContext.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(audioContext.destination);
    source.start();
}
