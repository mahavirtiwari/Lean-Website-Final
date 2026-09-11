import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AdminContactMessage,
  AdminMenuItem,
  AdminPage,
  AdminPageBlock,
  AdminPageListItem,
  AdminPostListItem,
  AdminSetting,
  AdminUser,
  ChangeStatusRequest,
  CreateUserRequest,
  Dashboard,
  MediaAsset,
  ReorderRequest,
  ResetPasswordRequest,
  SaveMenuItemRequest,
  SavePageBlockRequest,
  SavePageRequest,
  SavePostRequest,
  SaveSettingsRequest,
  UpdateContactMessageRequest,
  UpdateMediaAssetRequest,
  UpdateUserRequest,
  AuditLogEntry,
} from '../models/admin.models';
import {
  ContactMessageStatus,
  GalleryAlbum,
  GalleryAlbumSummary,
  GalleryImage,
  PagedQuery,
  PagedResult,
  Post,
  PostType,
  PublishStatus,
} from '../models/content.models';
import { ApiService, QueryValue } from './api.service';

/**
 * Typed access to the authenticated CMS API.
 *
 * The generic `resource<T, TRequest>()` factory backs the configuration-driven
 * management screens (banners, FAQs, scheme levels and so on), which all share
 * the same list / read / create / update / delete / reorder contract.
 */
@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly api = inject(ApiService);

  // ---------------------------------------------------------- dashboard ----

  getDashboard(): Observable<Dashboard> {
    return this.api.get<Dashboard>('admin/dashboard');
  }

  getActivity(query: PagedQuery & { entityName?: string } = {}): Observable<PagedResult<AuditLogEntry>> {
    return this.api.get<PagedResult<AuditLogEntry>>('admin/dashboard/activity', { ...query });
  }

  // -------------------------------------------------------------- pages ----

  getPages(
    query: PagedQuery & { status?: PublishStatus; parentId?: number } = {},
  ): Observable<PagedResult<AdminPageListItem>> {
    return this.api.get<PagedResult<AdminPageListItem>>('admin/pages', { ...query });
  }

  getPageTree(): Observable<AdminPageListItem[]> {
    return this.api.get<AdminPageListItem[]>('admin/pages/tree');
  }

  getPage(id: number): Observable<AdminPage> {
    return this.api.get<AdminPage>(`admin/pages/${id}`);
  }

  createPage(request: SavePageRequest): Observable<AdminPage> {
    return this.api.post<AdminPage>('admin/pages', request);
  }

  updatePage(id: number, request: SavePageRequest): Observable<AdminPage> {
    return this.api.put<AdminPage>(`admin/pages/${id}`, request);
  }

  setPageStatus(id: number, request: ChangeStatusRequest): Observable<void> {
    return this.api.post<void>(`admin/pages/${id}/status`, request);
  }

  deletePage(id: number): Observable<void> {
    return this.api.delete<void>(`admin/pages/${id}`);
  }

  reorderPages(request: ReorderRequest): Observable<void> {
    return this.api.post<void>('admin/pages/reorder', request);
  }

  // -------------------------------------------------------- page blocks ----

  getBlocks(pageId: number): Observable<AdminPageBlock[]> {
    return this.api.get<AdminPageBlock[]>(`admin/pages/${pageId}/blocks`);
  }

  createBlock(pageId: number, request: SavePageBlockRequest): Observable<AdminPageBlock> {
    return this.api.post<AdminPageBlock>(`admin/pages/${pageId}/blocks`, request);
  }

  updateBlock(pageId: number, blockId: number, request: SavePageBlockRequest): Observable<AdminPageBlock> {
    return this.api.put<AdminPageBlock>(`admin/pages/${pageId}/blocks/${blockId}`, request);
  }

  deleteBlock(pageId: number, blockId: number): Observable<void> {
    return this.api.delete<void>(`admin/pages/${pageId}/blocks/${blockId}`);
  }

  reorderBlocks(pageId: number, request: ReorderRequest): Observable<void> {
    return this.api.post<void>(`admin/pages/${pageId}/blocks/reorder`, request);
  }

  // -------------------------------------------------------------- posts ----

  getPosts(
    query: PagedQuery & { type?: PostType; status?: PublishStatus } = {},
  ): Observable<PagedResult<AdminPostListItem>> {
    return this.api.get<PagedResult<AdminPostListItem>>('admin/posts', { ...query });
  }

  getPost(id: number): Observable<Post> {
    return this.api.get<Post>(`admin/posts/${id}`);
  }

  createPost(request: SavePostRequest): Observable<Post> {
    return this.api.post<Post>('admin/posts', request);
  }

  updatePost(id: number, request: SavePostRequest): Observable<Post> {
    return this.api.put<Post>(`admin/posts/${id}`, request);
  }

  setPostStatus(id: number, request: ChangeStatusRequest): Observable<void> {
    return this.api.post<void>(`admin/posts/${id}/status`, request);
  }

  deletePost(id: number): Observable<void> {
    return this.api.delete<void>(`admin/posts/${id}`);
  }

  // --------------------------------------------------------------- menu ----

  getMenu(location?: string): Observable<AdminMenuItem[]> {
    return this.api.get<AdminMenuItem[]>('admin/menu', { location });
  }

  getMenuItem(id: number): Observable<AdminMenuItem> {
    return this.api.get<AdminMenuItem>(`admin/menu/${id}`);
  }

  createMenuItem(request: SaveMenuItemRequest): Observable<AdminMenuItem> {
    return this.api.post<AdminMenuItem>('admin/menu', request);
  }

  updateMenuItem(id: number, request: SaveMenuItemRequest): Observable<AdminMenuItem> {
    return this.api.put<AdminMenuItem>(`admin/menu/${id}`, request);
  }

  deleteMenuItem(id: number): Observable<void> {
    return this.api.delete<void>(`admin/menu/${id}`);
  }

  reorderMenu(request: ReorderRequest): Observable<void> {
    return this.api.post<void>('admin/menu/reorder', request);
  }

  // ------------------------------------------------------------ gallery ----

  getAlbums(query: PagedQuery = {}): Observable<PagedResult<GalleryAlbumSummary>> {
    return this.api.get<PagedResult<GalleryAlbumSummary>>('admin/gallery', { ...query });
  }

  getAlbum(id: number): Observable<GalleryAlbum> {
    return this.api.get<GalleryAlbum>(`admin/gallery/${id}`);
  }

  createAlbum(request: unknown): Observable<GalleryAlbum> {
    return this.api.post<GalleryAlbum>('admin/gallery', request);
  }

  updateAlbum(id: number, request: unknown): Observable<GalleryAlbum> {
    return this.api.put<GalleryAlbum>(`admin/gallery/${id}`, request);
  }

  deleteAlbum(id: number): Observable<void> {
    return this.api.delete<void>(`admin/gallery/${id}`);
  }

  addAlbumImage(albumId: number, request: unknown): Observable<GalleryImage> {
    return this.api.post<GalleryImage>(`admin/gallery/${albumId}/images`, request);
  }

  updateAlbumImage(albumId: number, imageId: number, request: unknown): Observable<GalleryImage> {
    return this.api.put<GalleryImage>(`admin/gallery/${albumId}/images/${imageId}`, request);
  }

  changeAlbumStatus(id: number, status: PublishStatus): Observable<void> {
    return this.api.post<void>(`admin/gallery/${id}/status`, { status });
  }

  deleteAlbumImage(albumId: number, imageId: number): Observable<void> {
    return this.api.delete<void>(`admin/gallery/${albumId}/images/${imageId}`);
  }

  // ---------------------------------------------------------- enquiries ----

  getEnquiries(
    query: PagedQuery & { status?: ContactMessageStatus; category?: string } = {},
  ): Observable<PagedResult<AdminContactMessage>> {
    return this.api.get<PagedResult<AdminContactMessage>>('admin/enquiries', { ...query });
  }

  getEnquiry(id: number): Observable<AdminContactMessage> {
    return this.api.get<AdminContactMessage>(`admin/enquiries/${id}`);
  }

  updateEnquiry(id: number, request: UpdateContactMessageRequest): Observable<AdminContactMessage> {
    return this.api.put<AdminContactMessage>(`admin/enquiries/${id}`, request);
  }

  deleteEnquiry(id: number): Observable<void> {
    return this.api.delete<void>(`admin/enquiries/${id}`);
  }

  exportEnquiries(status?: ContactMessageStatus): Observable<Blob> {
    return this.api.download('admin/enquiries/export', { status });
  }

  // ----------------------------------------------------------- settings ----

  getSettings(group?: string): Observable<AdminSetting[]> {
    return this.api.get<AdminSetting[]>('admin/settings', { group });
  }

  getSettingGroups(): Observable<string[]> {
    return this.api.get<string[]>('admin/settings/groups');
  }

  saveSettings(request: SaveSettingsRequest): Observable<void> {
    return this.api.put<void>('admin/settings', request);
  }

  // -------------------------------------------------------------- media ----

  getMedia(query: PagedQuery & { folder?: string } = {}): Observable<PagedResult<MediaAsset>> {
    return this.api.get<PagedResult<MediaAsset>>('admin/media', { ...query });
  }

  getMediaFolders(): Observable<string[]> {
    return this.api.get<string[]>('admin/media/folders');
  }

  uploadMedia(file: File, folder?: string, altText?: string, caption?: string): Observable<MediaAsset> {
    const form = new FormData();
    form.append('file', file, file.name);
    if (folder) form.append('folder', folder);
    if (altText) form.append('altText', altText);
    if (caption) form.append('caption', caption);

    return this.api.upload<MediaAsset>('admin/media/upload', form);
  }

  updateMedia(id: number, request: UpdateMediaAssetRequest): Observable<MediaAsset> {
    return this.api.put<MediaAsset>(`admin/media/${id}`, request);
  }

  deleteMedia(id: number): Observable<void> {
    return this.api.delete<void>(`admin/media/${id}`);
  }

  // -------------------------------------------------------------- users ----

  getUsers(query: PagedQuery = {}): Observable<PagedResult<AdminUser>> {
    return this.api.get<PagedResult<AdminUser>>('admin/users', { ...query });
  }

  getRoles(): Observable<string[]> {
    return this.api.get<string[]>('admin/users/roles');
  }

  getUser(id: string): Observable<AdminUser> {
    return this.api.get<AdminUser>(`admin/users/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<AdminUser> {
    return this.api.post<AdminUser>('admin/users', request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<AdminUser> {
    return this.api.put<AdminUser>(`admin/users/${id}`, request);
  }

  resetUserPassword(id: string, request: ResetPasswordRequest): Observable<void> {
    return this.api.post<void>(`admin/users/${id}/reset-password`, request);
  }

  deactivateUser(id: string): Observable<void> {
    return this.api.delete<void>(`admin/users/${id}`);
  }

  // ------------------------------------------------- generic CRUD access ----

  /**
   * Returns a typed client for one of the uniform admin resources.
   * `path` is the API segment, e.g. `banners` -> /api/admin/banners.
   */
  resource<TDto, TRequest>(path: string): AdminResourceClient<TDto, TRequest> {
    return {
      list: (query = {}) =>
        this.api.get<PagedResult<TDto>>(`admin/${path}`, query as Record<string, QueryValue>),
      get: (id: number) => this.api.get<TDto>(`admin/${path}/${id}`),
      create: (request: TRequest) => this.api.post<TDto>(`admin/${path}`, request),
      update: (id: number, request: TRequest) => this.api.put<TDto>(`admin/${path}/${id}`, request),
      remove: (id: number) => this.api.delete<void>(`admin/${path}/${id}`),
      reorder: (request: ReorderRequest) => this.api.post<void>(`admin/${path}/reorder`, request),
    };
  }
}

export interface AdminResourceClient<TDto, TRequest> {
  list(query?: PagedQuery): Observable<PagedResult<TDto>>;
  get(id: number): Observable<TDto>;
  create(request: TRequest): Observable<TDto>;
  update(id: number, request: TRequest): Observable<TDto>;
  remove(id: number): Observable<void>;
  reorder(request: ReorderRequest): Observable<void>;
}
