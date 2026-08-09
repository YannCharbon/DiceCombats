/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using Android.Content.Res;
using Android.Views;
using Android.Widget;
using System.Runtime.Versioning;
using static Android.Resource;
using Activity = Android.App.Activity;
using Rect = Android.Graphics.Rect;
using View = Android.Views.View;

namespace DiceCombats.Platform.Android;

// Version pinned to the project's own android minimum so the immersive flags,
// which need API 21, are not reported as unsupported.
[SupportedOSPlatform("Android24.0")]
public static class WebViewSoftInputPatch
{
    static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("Android Activity can't be null.");
    // Both stay null until Initialize has found the content view, so every use
    // below has to cope with the patch never having been wired up.
    static View? MChildOfContent;
    static FrameLayout.LayoutParams? FrameLayoutParams;
    static int UsableHeightPrevious = 0;

    public static void Initialize()
    {
        if (Activity.FindViewById(Id.Content) is not FrameLayout content)
        {
            return;
        }

        MChildOfContent = content.GetChildAt(0);
        if (MChildOfContent == null)
        {
            return;
        }

        if (MChildOfContent.ViewTreeObserver != null)
        {
            MChildOfContent.ViewTreeObserver.GlobalLayout += (s, o) => PossiblyResizeChildOfContent();
        }

        FrameLayoutParams = MChildOfContent.LayoutParameters as FrameLayout.LayoutParams;
    }

    static void PossiblyResizeChildOfContent()
    {
        if (MChildOfContent?.RootView == null)
        {
            return;
        }

        int usableHeightNow = ComputeUsableHeight();
        if (usableHeightNow != UsableHeightPrevious && FrameLayoutParams != null)
        {
            int usableHeightSansKeyboard = MChildOfContent.RootView.Height;
            int heightDifference = usableHeightSansKeyboard - usableHeightNow;
            if (heightDifference < 0)
            {
                usableHeightSansKeyboard = MChildOfContent.RootView.Width;
                heightDifference = usableHeightSansKeyboard - usableHeightNow;
            }

            if (heightDifference > usableHeightSansKeyboard / 4)
            {
                FrameLayoutParams.Height = usableHeightSansKeyboard - heightDifference;
            }
            else
            {
                FrameLayoutParams.Height = usableHeightNow;
            }
        }

        MChildOfContent.RequestLayout();
        UsableHeightPrevious = usableHeightNow;
    }

    static int ComputeUsableHeight()
    {
        Rect rect = new Rect();
        MChildOfContent?.GetWindowVisibleDisplayFrame(rect);
        if (IsImmersiveMode())
        {
            return rect.Bottom;
        }
        else
        {
            return rect.Bottom - GetStatusBarHeight();
        }
    }

    static int GetStatusBarHeight()
    {
        int result = 0;
        // var, not the type name: 'Resources' would bind to the project's own
        // DiceCombats.Resources namespace here.
        var resources = Activity.Resources;
        if (resources == null)
        {
            return result;
        }

        int resourceId = resources.GetIdentifier("status_bar_height", "dimen", "android");
        if (resourceId > 0)
        {
            result = resources.GetDimensionPixelSize(resourceId);
        }
        return result;
    }

    static bool IsImmersiveMode()
    {
        View? decorView = Activity.Window?.DecorView;
        if (decorView == null)
        {
            return false;
        }

        // Deprecated from API 30 in favour of WindowInsetsController, but kept on
        // purpose: the flags still read back correctly, and this only decides
        // whether the status bar height counts towards the usable height.
#pragma warning disable CA1422
        return (decorView.SystemUiFlags & SystemUiFlags.Immersive) == SystemUiFlags.Immersive;
#pragma warning restore CA1422
    }
}
