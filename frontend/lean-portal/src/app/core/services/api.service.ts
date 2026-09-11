import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Values acceptable as a query-string parameter before serialisation. */
export type QueryValue = string | number | boolean | null | undefined;

/**
 * Thin wrapper over HttpClient that prefixes the configured API base URL and
 * drops empty query-string parameters, so callers can pass filter objects straight
 * from a form without pruning them first.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl.replace(/\/+$/, '');

  get<T>(path: string, query?: Record<string, QueryValue>): Observable<T> {
    return this.http.get<T>(this.url(path), { params: this.toParams(query) });
  }

  post<T>(path: string, body?: unknown): Observable<T> {
    return this.http.post<T>(this.url(path), body ?? {});
  }

  put<T>(path: string, body?: unknown): Observable<T> {
    return this.http.put<T>(this.url(path), body ?? {});
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(this.url(path));
  }

  /** Multipart upload; the browser sets the boundary so no Content-Type is added. */
  upload<T>(path: string, form: FormData): Observable<T> {
    return this.http.post<T>(this.url(path), form);
  }

  /** Fetches a file (CSV export, generated document) as a Blob. */
  download(path: string, query?: Record<string, QueryValue>): Observable<Blob> {
    return this.http.get(this.url(path), {
      params: this.toParams(query),
      responseType: 'blob',
    });
  }

  /** Absolute URL for a server-relative asset path such as an upload. */
  asset(path: string | null | undefined): string {
    if (!path) return '';
    if (/^(https?:)?\/\//i.test(path) || path.startsWith('data:')) return path;
    return `${this.base.replace(/\/api$/, '')}/${path.replace(/^\/+/, '')}`;
  }

  /** The full address of an API path, for places that need a URL rather than a request - a frame. */
  absolute(path: string): string {
    return this.url(path);
  }

  private url(path: string): string {
    return `${this.base}/${path.replace(/^\/+/, '')}`;
  }

  private toParams(query?: Record<string, QueryValue>): HttpParams {
    let params = new HttpParams();
    if (!query) return params;

    for (const [key, value] of Object.entries(query)) {
      if (value === null || value === undefined || value === '') continue;
      params = params.set(key, String(value));
    }

    return params;
  }
}
