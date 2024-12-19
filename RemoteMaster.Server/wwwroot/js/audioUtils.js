let audioContext = null;
export function initAudioContext() {
    if (!audioContext) {
        audioContext = new AudioContext();
        audioContext.resume();
    }
}
function convertPCMToFloat32(audioData) {
    const samples = audioData.length / 4;
    const float32Data = new Float32Array(samples);
    const dataView = new DataView(audioData.buffer);
    for (let i = 0; i < samples; i++) {
        const int32 = dataView.getInt32(i * 4, true);
        float32Data[i] = int32 / 2147483648;
    }
    return float32Data;
}
export async function playAudioChunk(audioData) {
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
        const sampleRate = 48000;
        const numberOfChannels = 2;
        const float32Data = convertPCMToFloat32(audioData);
        if (float32Data.length === 0) {
            console.error("No valid audio data to process.");
            return;
        }
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
    }
    catch (error) {
        console.error("Error decoding and playing audio chunk:", error);
    }
}
//# sourceMappingURL=audioUtils.js.map