/**
 * Development configuration.
 *
 * `apiUrl` points at the local ASP.NET Core host. In production the API is
 * served from the same origin as the site (IIS reverse proxy / sub-application),
 * so the production file uses a relative path and no CORS is involved.
 */
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5199/api',
  /** Origin of the transactional LEAN application that the login tiles deep-link to. */
  leanAppOrigin: 'https://lean.msme.gov.in',
  siteName: 'MSME Competitive (LEAN) Scheme',
};
