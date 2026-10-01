// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ClientImport;

public record ClientImportTemplateFile(byte[] Content, string FileName, string ContentType);
