let audioContext = null;
let audioAccumulator = [];
let playTimer = null;
const CHUNK_ACCUMULATION_COUNT = 5;
const SAMPLE_RATE = 48000;
const NUMBER_OF_CHANNELS = 2;
const PLAY_INTERVAL_MS = 500;
export function initAudioContext() {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    if (audioContext.state === 'suspended') {
        audioContext.resume().catch(err => {
            console.warn("AudioContext resume attempt failed:", err);
        });
    }
}
export function resumeAudioContext() {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    if (audioContext.state === 'suspended') {
        audioContext.resume().catch(err => {
            console.error("Failed to resume AudioContext:", err);
        });
    }
}
function base64ToUint8Array(base64) {
    const binaryString = atob(base64);
    const length = binaryString.length;
    const bytes = new Uint8Array(length);
    for (let i = 0; i < length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes;
}
export async function playAudioChunk(audioDataBase64) {
    initAudioContext();
    if (!audioContext) {
        console.error("AudioContext is not initialized.");
        return;
    }
    const audioData = base64ToUint8Array(audioDataBase64);
    if (audioData.length === 0) {
        return;
    }
    const samples = audioData.length / 4;
    const float32Data = new Float32Array(samples);
    const dataView = new DataView(audioData.buffer);
    for (let i = 0; i < samples; i++) {
        float32Data[i] = dataView.getFloat32(i * 4, true);
    }
    audioAccumulator.push(float32Data);
    if (playTimer !== null) {
        clearTimeout(playTimer);
    }
    playTimer = window.setTimeout(playAccumulatedChunks, PLAY_INTERVAL_MS);
    if (audioAccumulator.length >= CHUNK_ACCUMULATION_COUNT) {
        playAccumulatedChunks();
    }
}
function playAccumulatedChunks() {
    if (!audioContext || audioAccumulator.length === 0) {
        return;
    }
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
    audioAccumulator = [];
    const samplesPerChannel = Math.floor(combined.length / NUMBER_OF_CHANNELS);
    const audioBuffer = audioContext.createBuffer(NUMBER_OF_CHANNELS, samplesPerChannel, SAMPLE_RATE);
    for (let ch = 0; ch < NUMBER_OF_CHANNELS; ch++) {
        const channelData = audioBuffer.getChannelData(ch);
        for (let i = 0; i < samplesPerChannel; i++) {
            channelData[i] = combined[i * NUMBER_OF_CHANNELS + ch];
        }
    }
    const source = audioContext.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(audioContext.destination);
    source.start();
}
//# sourceMappingURL=audioUtils.js.map