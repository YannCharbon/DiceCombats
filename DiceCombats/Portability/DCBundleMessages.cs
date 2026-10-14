/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using System.Globalization;
using Microsoft.Extensions.Localization;

namespace DiceCombats.Portability;

/// <summary>The texts (Portability.resx) the pages show after an export or an import.</summary>
public static class DCBundleMessages
{
    public static string ReadError(IStringLocalizer loc, DCBundleReadException error, long fileSizeBytes) => error.Error switch
    {
        DCBundleReadError.OtherAppFile => loc["ErrorOtherAppFile"],
        DCBundleReadError.NewerFormat => loc["ErrorNewerFormat"],
        DCBundleReadError.FileTooLarge => string.Format(loc["ErrorFileTooLarge"],
            Math.Ceiling(fileSizeBytes / (1024d * 1024d)).ToString(CultureInfo.CurrentCulture),
            (DCBundle.MaxSizeBytes / (1024 * 1024)).ToString(CultureInfo.CurrentCulture)),
        _ => loc["ErrorUnreadableFile"],
    };
}
