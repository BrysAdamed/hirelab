import { Injectable, inject } from '@angular/core';
import { type ApiResult, apiFailure } from '../api/api-result';
import { type Attachment, type UploadedAttachment, STAFF_API } from '../api/staff-api';

/** The largest file the API accepts (ADR 0005). */
export const maxAttachmentSize = 10 * 1024 * 1024;

/** The most attachments a candidate can have (ADR 0005). */
export const maxAttachmentsPerCandidate = 10;

/** The file types the API accepts, for the file picker. The API checks the content too. */
export const acceptedAttachmentTypes = '.pdf,.docx,.txt,.md,.png,.jpg,.jpeg';

const tooLargeMessage = 'The file is larger than 10 MB.';

/** The attachment endpoints of a candidate. */
@Injectable({ providedIn: 'root' })
export class AttachmentsService {
  private readonly api = inject(STAFF_API);

  /** The candidate's attachments, oldest upload first. */
  async list(candidateId: string): Promise<ApiResult<Attachment[]>> {
    const { data, error } = await this.api.GET('/api/staff/candidates/{candidateId}/attachments', {
      params: { path: { candidateId } },
    });
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Uploads a file as a multipart form. The API refuses files over 10 MB, other types and an 11th file. */
  async upload(candidateId: string, file: File): Promise<ApiResult<UploadedAttachment>> {
    if (file.size > maxAttachmentSize) {
      return { ok: false, message: tooLargeMessage };
    }

    const { data, error, response } = await this.api.POST(
      '/api/staff/candidates/{candidateId}/attachments',
      {
        params: { path: { candidateId } },
        body: { file },
        bodySerializer: (body) => {
          const form = new FormData();
          form.append('file', body.file, file.name);
          return form;
        },
      },
    );
    if (data) {
      return { ok: true, value: data };
    }
    return response.status === 413 ? { ok: false, message: tooLargeMessage } : apiFailure(error);
  }

  /** The attachment's content. */
  async download(candidateId: string, attachmentId: string): Promise<ApiResult<Blob>> {
    const { data, error } = await this.api.GET(
      '/api/staff/candidates/{candidateId}/attachments/{attachmentId}',
      { params: { path: { candidateId, attachmentId } }, parseAs: 'blob' },
    );
    return data ? { ok: true, value: data } : apiFailure(error);
  }

  /** Deletes an attachment for good. */
  async delete(candidateId: string, attachmentId: string): Promise<ApiResult<void>> {
    const { error, response } = await this.api.DELETE(
      '/api/staff/candidates/{candidateId}/attachments/{attachmentId}',
      { params: { path: { candidateId, attachmentId } } },
    );
    return response.ok ? { ok: true, value: undefined } : apiFailure(error);
  }
}
