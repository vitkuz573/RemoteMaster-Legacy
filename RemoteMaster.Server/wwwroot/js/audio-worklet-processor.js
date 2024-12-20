"use strict";
class StreamingProcessor extends AudioWorkletProcessor {
    queue;
    constructor() {
        super();
        this.queue = [];
        this.port.onmessage = (event) => {
            const chunk = event.data;
            this.queue.push(chunk);
        };
    }
    process(inputs, outputs, parameters) {
        const output = outputs[0];
        const channelCount = output.length;
        if (channelCount === 0)
            return true;
        const blockSize = output[0].length;
        const samplesNeeded = blockSize * channelCount;
        let samplesFilled = 0;
        while (this.queue.length > 0 && samplesFilled < samplesNeeded) {
            const currentChunk = this.queue[0];
            const samplesToCopy = Math.min(samplesNeeded - samplesFilled, currentChunk.length);
            for (let i = 0; i < samplesToCopy / channelCount; i++) {
                for (let ch = 0; ch < channelCount; ch++) {
                    output[ch][(samplesFilled / channelCount) + i] = currentChunk[(i * channelCount) + ch];
                }
            }
            samplesFilled += samplesToCopy;
            if (samplesToCopy < currentChunk.length) {
                this.queue[0] = currentChunk.slice(samplesToCopy);
            }
            else {
                this.queue.shift();
            }
        }
        while (samplesFilled < samplesNeeded) {
            for (let ch = 0; ch < channelCount; ch++) {
                output[ch][samplesFilled / channelCount] = 0;
            }
            samplesFilled += channelCount;
        }
        return true;
    }
}
registerProcessor('streaming-processor', StreamingProcessor);
//# sourceMappingURL=audio-worklet-processor.js.map