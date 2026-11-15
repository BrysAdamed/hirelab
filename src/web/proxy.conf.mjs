const target = process.env['API_URL'] ?? 'http://localhost:5291';

export default {
  '/api': { target, secure: false },
  '/hubs': { target, secure: false, ws: true },
};
