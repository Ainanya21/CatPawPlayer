"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const fastify_1 = require("fastify");
const server = (0, fastify_1.default)({ logger: false });
server.get('/health', async () => {
    return { status: 'ok', time: Date.now() };
});
server.post('/init', async (req) => {
    return {
        code: 0,
        sites: [
            { key: 'wogg', name: '玩偶4K', type: 3, api: 'csp_Wogg' },
            { key: 'zhizhen', name: '至臻4K', type: 3, api: 'csp_Zhizhen' },
            { key: 'feisu', name: '飞速资源', type: 1, api: 'https://www.feisuzy.com/api.php/provide/vod/' },
        ],
    };
});
const PORT = 9988;
server.listen({ port: PORT, host: '127.0.0.1' }, (err, address) => {
    if (err) {
        console.error('[ProxyServer Error]', err);
    }
    else {
        console.log(`[ProxyServer] Running on ${address}`);
    }
});
