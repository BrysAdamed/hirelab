import { TestBed } from '@angular/core/testing';
import { fireEvent, render, screen } from '@testing-library/angular';
import { type Attachment, STAFF_API_FETCH } from '../api/staff-api';
import { FakeFetch, json, noContent } from '../testing/fake-fetch';
import { CandidateAttachments, formatFileSize } from './candidate-attachments';

describe('CandidateAttachments', () => {
  const candidateId = '0190a5c4-7e3d-7b8a-9f21-000000000051';
  const path = `/api/staff/candidates/${candidateId}/attachments`;
  const cv: Attachment = {
    id: '0190a5c4-7e3d-7b8a-9f21-000000000061',
    fileName: 'Fake CV.pdf',
    contentType: 'application/pdf',
    size: 245_760,
    uploadedAt: '2026-01-06T09:00:00Z',
    uploadedById: '0190a5c4-7e3d-7b8a-9f21-000000000071',
    uploadedByName: 'Ivy Interviewer',
  };

  let fake: FakeFetch;
  let listed: Attachment[];

  async function renderCard(canChange: boolean) {
    await render(CandidateAttachments, {
      inputs: { candidateId, canChange },
      providers: [{ provide: STAFF_API_FETCH, useValue: fake.fetch }],
    });
  }

  beforeEach(() => {
    listed = [cv];
    fake = new FakeFetch().on('GET', path, () => json(200, listed));
  });

  afterEach(() => TestBed.resetTestingModule());

  it('lists the files with their size and who uploaded them', async () => {
    // Act
    await renderCard(true);

    // Assert
    expect(await screen.findByText('Fake CV.pdf')).toBeTruthy();
    expect(screen.getByText('240 KB')).toBeTruthy();
    expect(screen.getByText(/by Ivy Interviewer/)).toBeTruthy();
    expect(screen.getByText(/1 of 10 files/)).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Download Fake CV.pdf' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Delete Fake CV.pdf' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Upload file' })).toBeTruthy();
  });

  it('offers only downloads when the candidate cannot be changed', async () => {
    // Act
    await renderCard(false);

    // Assert
    expect(await screen.findByRole('button', { name: 'Download Fake CV.pdf' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Upload file' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Delete Fake CV.pdf' })).toBeNull();
    expect(screen.queryByText(/of 10 files/)).toBeNull();
  });

  it('says there are no attachments without suggesting more will come when the candidate cannot be changed', async () => {
    // Arrange
    listed = [];

    // Act
    await renderCard(false);

    // Assert
    expect(await screen.findByText('No attachments.')).toBeTruthy();
    expect(screen.queryByText('No attachments yet.')).toBeNull();
  });

  it('uploads the chosen file as the multipart field "file" and shows it', async () => {
    // Arrange
    let uploadedBody = '';
    let uploadedContentType: string | null = null;
    fake.on('POST', path, async (request) => {
      uploadedContentType = request.headers.get('Content-Type');
      uploadedBody = await request.text();
      listed = [...listed, { ...cv, id: 'uploaded', fileName: 'notes.md' }];
      return json(201, {
        id: 'uploaded',
        fileName: 'notes.md',
        contentType: 'text/markdown',
        size: 6,
      });
    });
    await renderCard(true);
    await screen.findByText('Fake CV.pdf');
    const input = document.querySelector<HTMLInputElement>('input[type=file]')!;

    // Act
    fireEvent.change(input, { target: { files: [new File(['# Fake'], 'notes.md')] } });

    // Assert
    expect(await screen.findByText('notes.md')).toBeTruthy();
    expect(uploadedContentType).toMatch(/^multipart\/form-data; boundary=/);
    expect(uploadedBody).toContain('Content-Disposition: form-data; name="file"');
  });

  it('shows why an upload was refused', async () => {
    // Arrange
    fake.on('POST', path, () =>
      json(400, {
        status: 400,
        detail: 'Only PDF, DOCX, TXT, MD, PNG, JPG files can be uploaded.',
        code: 'Attachment.UnsupportedType',
      }),
    );
    await renderCard(true);
    await screen.findByText('Fake CV.pdf');
    const input = document.querySelector<HTMLInputElement>('input[type=file]')!;

    // Act
    fireEvent.change(input, { target: { files: [new File(['MZ'], 'program.exe')] } });

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('Only PDF, DOCX');
  });

  it('refuses a file over 10 MB without uploading it', async () => {
    // Arrange
    await renderCard(true);
    await screen.findByText('Fake CV.pdf');
    const input = document.querySelector<HTMLInputElement>('input[type=file]')!;
    const huge = new File(['x'], 'huge.pdf');
    Object.defineProperty(huge, 'size', { value: 10 * 1024 * 1024 + 1 });

    // Act
    fireEvent.change(input, { target: { files: [huge] } });

    // Assert
    expect((await screen.findByRole('alert')).textContent).toContain('larger than 10 MB');
    expect(fake.requests.some((request) => request.method === 'POST')).toBe(false);
  });

  it('deletes a file after confirmation', async () => {
    // Arrange
    fake.on('DELETE', `${path}/${cv.id}`, () => {
      listed = [];
      return noContent();
    });
    await renderCard(true);

    // Act
    fireEvent.click(await screen.findByRole('button', { name: 'Delete Fake CV.pdf' }));
    fireEvent.click(await screen.findByRole('button', { name: 'Delete file' }));

    // Assert
    expect(await screen.findByText('No attachments yet.')).toBeTruthy();
  });

  it('formats file sizes for people', () => {
    expect(formatFileSize(512)).toBe('512 B');
    expect(formatFileSize(2048)).toBe('2 KB');
    expect(formatFileSize(1572864)).toBe('1.5 MB');
  });
});
