import http from 'k6/http';

const BASE = __ENV.BASE_URL || 'http://localhost:5079';

export default function () {
    if (__ENV.MODE === 'hit') {
        http.get(`${BASE}/envelopes?page=1&pageSize=20`);
    } else {
        // Nhánh miss sinh một số trang ngẫu nhiên rất lớn trước,
        const page = Math.floor(Math.random() * 1000000) + 1;
        http.get(`${BASE}/envelopes?page=${page}&pageSize=20`);
    }
}
