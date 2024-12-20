let audioContext = null;
let workletNode = null;
const SAMPLE_RATE = 48000;
const NUMBER_OF_CHANNELS = 2;
export async function initAudioContext() {
    if (!audioContext) {
        audioContext = new AudioContext({ sampleRate: SAMPLE_RATE });
    }
    if (audioContext.state === 'suspended') {
        await audioContext.resume();
    }
    await audioContext.audioWorklet.addModule('js/audio-worklet-processor.js');
    workletNode = new AudioWorkletNode(audioContext, 'streaming-processor', {
        numberOfInputs: 0,
        numberOfOutputs: 1,
        outputChannelCount: [NUMBER_OF_CHANNELS]
    });
    workletNode.connect(audioContext.destination);
}
export async function resumeAudioContext() {
    if (audioContext && audioContext.state === 'suspended') {
        await audioContext.resume();
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
    if (!audioContext || !workletNode) {
        console.error("AudioContext or Worklet not initialized. Call initAudioContext() first.");
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
    workletNode.port.postMessage(float32Data);
}
//# sourceMappingURL=audioUtils.js.map