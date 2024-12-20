let audioContext: AudioContext | null = null;
let workletNode: AudioWorkletNode | null = null;

const SAMPLE_RATE = 48000;
const NUMBER_OF_CHANNELS = 2;

/**
 * Initializes AudioContext and loads the AudioWorklet module.
 * Call this once after user gesture.
 */
export async function initAudioContext(): Promise<void> {
    if (!audioContext) {
        audioContext = new AudioContext({ sampleRate: SAMPLE_RATE });
    }
    if (audioContext.state === 'suspended') {
        await audioContext.resume();
    }

    // Load the AudioWorklet script compiled from audio-worklet-processor.ts to JS
    // Make sure 'audio-worklet-processor.js' is accessible (e.g. in the same directory)
    await audioContext.audioWorklet.addModule('js/audio-worklet-processor.js');

    workletNode = new AudioWorkletNode(audioContext, 'streaming-processor', {
        numberOfInputs: 0,
        numberOfOutputs: 1,
        outputChannelCount: [NUMBER_OF_CHANNELS]
    });

    workletNode.connect(audioContext.destination);
}

/**
 * Resumes the AudioContext if needed.
 * Should be called after a user gesture if the context is suspended.
 */
export async function resumeAudioContext(): Promise<void> {
    if (audioContext && audioContext.state === 'suspended') {
        await audioContext.resume();
    }
}

/**
 * Decodes a base64 string to Uint8Array.
 * @param base64 base64 encoded string
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
 * Converts base64-encoded float32 PCM samples into a Float32Array
 * and sends them to the AudioWorkletProcessor for smooth, continuous playback.
 * @param audioDataBase64 Base64 encoded float32 PCM data
 */
export async function playAudioChunk(audioDataBase64: string): Promise<void> {
    if (!audioContext || !workletNode) {
        console.error("AudioContext or Worklet not initialized. Call initAudioContext() first.");
        return;
    }

    const audioData = base64ToUint8Array(audioDataBase64);
    if (audioData.length === 0) {
        return; // empty data, ignore
    }

    const samples = audioData.length / 4;
    const float32Data = new Float32Array(samples);
    const dataView = new DataView(audioData.buffer);

    for (let i = 0; i < samples; i++) {
        float32Data[i] = dataView.getFloat32(i * 4, true);
    }

    // Send samples to the worklet
    workletNode.port.postMessage(float32Data);
}
