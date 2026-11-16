const target = process.env['API_URL'] ?? 'https://localhost:7291';

export default {
  '/api': { target, secure: false },
  '/hubs': { target, secure: false, ws: true },
};
