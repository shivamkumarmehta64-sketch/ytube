using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackTube
{
    public static class AdShieldEngine
    {
        private static readonly string[] AdDomains = new string[]
        {
            "doubleclick.net", "googlesyndication.com", "googleadservices.com",
            "google-analytics.com", "googletagmanager.com", "pagead2.googlesyndication.com",
            "2mdn.net", "tpc.googlesyndication.com", "adservice.google.com",
            "adsafeprotected.com", "moatads.com", "moat.com", "adsrvr.org",
            "serving-sys.com", "casalemedia.com", "rfihub.com", "openx.net",
            "pubmatic.com", "rubiconproject.com", "indexww.com", "criteo.com",
            "criteo.net", "taboola.com", "outbrain.com", "scorecardresearch.com",
            "exelator.com", "adnxs.com", "advertising.com", "yieldmo.com",
            "sharethrough.com", "improvedigital.com", "smartadserver.com",
            "adform.net", "adzerk.net", "media.net", "contextweb.com",
            "amazon-adsystem.com", "aax.amazon-adsystem.com", "sonobi.com",
            "appnexus.com", "bluekai.com", "demdex.net", "adsymptotic.com",
            "bat.bing.com", "pixel.quantserve.com", "pixel.rubiconproject.com",
            "pixel.moat.com", "pixel.adsafeprotected.com", "tracking.adsrvr.org",
            "bttrack.com", "adserver.adtech.de", "lon-ads.adapt.tv",
            "ad-apac.doubleclick.net", "ad-uk.doubleclick.net", "ad.doubleclick.net",
            "ad-g.doubleclick.net", "cm.g.doubleclick.net", "pubads.g.doubleclick.net",
            "securepubads.g.doubleclick.net", "adclick.g.doubleclick.net",
            "pagead.l.doubleclick.net", "partner.googleadservices.com",
            "stats.g.doubleclick.net", "adservice.google.co.in",
            "adservice.google.co.uk", "adservice.google.de", "adservice.google.fr",
            "adservice.google.ca", "adservice.google.com.au", "adservice.google.co.jp",
            "adservice.google.es", "adservice.google.it", "adservice.google.nl",
            "adservice.google.com.br", "adservice.google.com.mx", "adservice.google.ru",
            "www.googletagservices.com", "partneradss.dl.google.com",
            "googleads.g.doubleclick.net", "marketingplatform.google.com",
            "analytics.google.com", "region1.google-analytics.com",
            "ssl.google-analytics.com", "connect.facebook.net", "pixel.facebook.com",
            "an.facebook.com", "staticxx.facebook.com", "hotjar.com", "mixpanel.com",
            "adzerk.net", "engine.adzerk.net", "quantserve.com", "quantummetric.com",
            "smartlook.com", "snapads.com", "snapchat.com/ads", "popads.net",
            "popcash.net", "propellerads.com", "revcontent.com", "zergnet.com",
            "youtube.com/api/stats/ads", "youtube.com/pagead/",
            "youtube.com/get_midroll_info", "youtube.com/api/stats/"
        };

        private static readonly string[] AllowedStreamDomains = new string[]
        {
            "googlevideo.com", "ytimg.com", ".m3u8", ".mpd", "blob:"
        };

        public const string CoreAdShieldScript = @"
(function() {
    'use strict';
    if (window.__blackTubeShieldActive) return;
    window.__blackTubeShieldActive = true;

    // ── 1. Comprehensive ad keys list for JSON response stripping ──
    var adKeys = [
        'adPlacements','adSlots','playerAds','adBreak','adBreakHeartbeatParams',
        'frameworkUpdates','auxiliaryUi.messageRenderers.upsellDialogRenderer',
        'promotedSparklesWebRenderer','promotedVideoRenderer',
        'compactPromotedVideoRenderer','compactPromotedItemRenderer',
        'backgroundPromoRenderer','statementBannerRenderer',
        'brandVideoShelfRenderer','inlineAdLayoutRenderer','adSlotRenderer',
        'adBreakParams','adPlacementId','adSlotId','adVideoId',
        'playerAdParams','adParams','adTagUrl','adTagUrls',
        'companionAd','instreamVideoAd','overlayAd','promotedUrl',
        'promotedSparklesTextRenderer','promotedSparklesVideosRenderer',
        'searchPyvRenderer','actionCompanionAdRenderer','displayAdRenderer',
        'videoMastheadAdRenderer','mastheadAdRenderer','mastheadAd',
        'midrolls','prerolls','postrolls','adBreakOffset','adBreakDuration',
        'adBreakType','adBreakList','adBreakIndex','adBreakCount',
        'adBreakInfo','adBreakSection','adBreakTiming','adBreakStart','adBreakEnd',
        'adBreakStatus','adBreakPosition','adBreakSequence','adBreakGroup',
        'adBreakSegment','adBreakMetadata','adBreakTracking','adBreakEvent',
        'adBreakState','adBreakConfig','adBreakPolicy','adBreakRule',
        'adBreakSchedule','adBreakSlot','adBreakTemplate','adBreakVariant',
        'adBreakVersion','adBreakWarning','adBreakZone','adIsActive','adIsPlaying',
        'adIsPaused','adIsSkippable','adIsSkipped','adIsCompleted','adIsBlocked',
        'adType','adMode','adFormat','adSource','adNetwork','adUnit','adServer',
        'adCampaign','adGroup','adCreative','adViewability','adEngagement',
        'adInteraction','cumulativeAds','adCount','totalAds','remainingAds',
        'adSegment','adChapter','adMarker','adTimeline','adOverlay','adBanner',
        'adPopup','adSlide','adInterstitial','adFullscreen','adMinimized',
        'adAudio','adVideo','adImage','adText','adRich','adFeedback','adSkip',
        'adDismiss','adClose','adSettings','adPreferences','adPrivacy',
        'adPersonalization','adTargeting','adAttribution','adMeasurability',
        'carouselAdRenderer','carouselAdRendererViewModel',
        'carouselAdRendererViewModelGrid','carouselAdRendererViewModelList',
        'searchAdsRenderer','searchAdsRendererViewModel','adSlot',
        'masthead','sparkles','promoted','promo','promotion',
        'mealbar','legalBanner','enforcementMessage','bannerPromo','displayAd',
        'actionCompanion','inFeedAd','feedAd','shelfAd','sectionListAd',
        'adInfoDialog','adPreview','adDisclaimer','adChoices','adSelector',
        'adCreative','adMetadata','brandInteraction','brandSurvey','brandVideo',
        'ctaOverlay','ctaBanner','ctaButton','skippableAd','nonSkippableAd',
        'hotelAd','flightAd','productAd','shoppingAd','subscriptionAd',
        'trialAd','upsellAd','ypcAd','ypcGetPremium','ypcPurchase','getPremium',
        'premiumLabel','premiumDialog','musicPass','musicAd','musicBanner',
        'reelAd','reelShelfAd','shortsAd'
    ];

    function deepClean(obj) {
        if (!obj || typeof obj !== 'object') return false;
        if (Array.isArray(obj)) {
            var modified = false;
            for (var i = 0; i < obj.length; i++) {
                if (deepClean(obj[i])) modified = true;
            }
            return modified;
        }
        var cleaned = false;
        for (var k = 0; k < adKeys.length; k++) {
            var key = adKeys[k];
            if (key in obj) {
                delete obj[key];
                cleaned = true;
            }
        }
        for (var prop in obj) {
            if (Object.prototype.hasOwnProperty.call(obj, prop)) {
                if (deepClean(obj[prop])) cleaned = true;
            }
        }
        return cleaned;
    }

    // ── 2. Intercept JSON.parse for YouTube initial payloads ──
    try {
        var _origParse = JSON.parse;
        var _fastPattern = /youtube|ytInitial|playerResponse|ytcfg|ytplayer|innertube|music\.youtube/i;
        JSON.parse = function(text, reviver) {
            var str = text ? String(text) : '';
            if (str.length < 50 || str.length > 800000 || !_fastPattern.test(str)) {
                return _origParse.apply(this, arguments);
            }
            try {
                var parsed = _origParse.apply(this, arguments);
                if (parsed && typeof parsed === 'object') deepClean(parsed);
                return parsed;
            } catch(e) {
                return _origParse.apply(this, arguments);
            }
        };
    } catch(e) {}

    // ── 3. Intercept window.fetch for /youtubei/v1/ responses ──
    try {
        var _origFetch = window.fetch;
        if (_origFetch) {
            window.fetch = function(input, init) {
                var url = typeof input === 'string' ? input : (input && input.url ? input.url : '');
                if (/\/youtubei\/v1\//.test(url)) {
                    return _origFetch.apply(this, arguments).then(function(res) {
                        if (!res.ok) return res;
                        var contentType = res.headers.get('content-type') || '';
                        if (!/json/.test(contentType)) return res;
                        return res.clone().text().then(function(txt) {
                            try {
                                var data = _origParse(txt);
                                if (deepClean(data)) {
                                    return new Response(JSON.stringify(data), {
                                        status: res.status,
                                        statusText: res.statusText,
                                        headers: res.headers
                                    });
                                }
                            } catch(e) {}
                            return res;
                        }).catch(function() { return res; });
                    });
                }
                return _origFetch.apply(this, arguments);
            };
        }
    } catch(e) {}

    // ── 4. Intercept XMLHttpRequest for /youtubei/v1/ ──
    try {
        var _origXhrOpen = XMLHttpRequest.prototype.open;
        var _origXhrSend = XMLHttpRequest.prototype.send;
        XMLHttpRequest.prototype.open = function(method, url) {
            this._reqUrl = url;
            return _origXhrOpen.apply(this, arguments);
        };
        XMLHttpRequest.prototype.send = function() {
            if (this._reqUrl && /\/youtubei\/v1\//.test(this._reqUrl)) {
                var self = this;
                var origOnLoad = self.onload;
                self.addEventListener('load', function() {
                    try {
                        var ct = self.getResponseHeader('content-type') || '';
                        if (/json/.test(ct) && self.responseText && self.responseText.length > 50) {
                            var json = _origParse(self.responseText);
                            if (deepClean(json)) {
                                Object.defineProperty(self, 'responseText', { value: JSON.stringify(json), writable: false });
                            }
                        }
                    } catch(e) {}
                    if (origOnLoad) origOnLoad.apply(self, arguments);
                });
            }
            return _origXhrSend.apply(this, arguments);
        };
    } catch(e) {}

    // ── 5. Instant CSS stylesheet injection ──
    function injectAdCSS() {
        if (document.getElementById('blacktube-adblock-css')) return;
        var style = document.createElement('style');
        style.id = 'blacktube-adblock-css';
        style.textContent = [
            '.ytp-ad-progress,.ytp-ad-progress-list,.ytp-ad-image-overlay',
            '.ytp-ad-player-overlay,.ytp-ad-overlay-container,.ytp-ad-module',
            '.ytp-ad-badge-overlay,.ytp-ad-survey-player-overlay,.ytp-ad-preview-container',
            '.ytp-ad-text-overlay,.ytp-ad-message-container,.ytp-ad-skip-button-container',
            '.ytp-ad-skip-button,.ytp-ad-skip-button-modern,.ytp-suggested-action-badge',
            '.ytp-ad-skip-button-slot,.ytp-ad-skip-button-container-slot',
            '#masthead-ad,#masthead-ad-container,ytd-masthead-ad,[masthead-ad]',
            'ytd-ad-slot-renderer,ytd-video-masthead-ad-advertiser-info-renderer',
            'ytd-in-feed-ad-layout-renderer,ytd-banner-promo-renderer',
            'ytd-promoted-video-renderer,ytd-compact-promoted-video-renderer',
            'ytd-action-companion-ad-renderer,ytd-display-ad-renderer',
            'ytd-statement-banner-renderer,ytd-promoted-sparkles-web-renderer',
            'ytd-rich-section-renderer:has(ytd-ad-slot-renderer)',
            'ytd-rich-item-renderer:has([is-ad]),ytd-rich-item-renderer:has([data-is-ad])',
            'ytd-rich-item-renderer:has(ytd-ad-slot-renderer),ytd-shelf-renderer:has(ytd-ad-slot-renderer)',
            '#merch-shelf,#promotion-shelf,#offer-shelf,#player-ads,#player-message',
            'ytd-mealbar-promo-renderer,ytd-get-premium,ytd-premium-label',
            'ytd-enforcement-message-view-model,ytd-legal-banner',
            'tp-yt-paper-dialog:has(ytd-mealbar-promo-renderer)',
            'ytd-popup-container:has(ytd-mealbar-promo-renderer)',
            'ytd-popup-container:has(ytd-enforcement-message-view-model)',
            'ytmusic-mealbar-promo-renderer,ytmusic-ad-slot-renderer',
            'ytmusic-banner-promo-renderer,ytmusic-display-ad-renderer',
            'ytmusic-pivot-bar-renderer:has(ytmusic-ad-slot-renderer)',
            '.ytmusic-mealbar-promo,.ytmusic-ad-slot,ytmusic-ad-slot,[is-music-ad]',
            'ytd-shorts-ad,ytd-reel-ad,ytd-reel-shelf-renderer:has(ytd-ad-slot-renderer)',
            '[data-is-ad],[is-ad],[data-ad],[ad-data]'
        ].join(',') + '{display:none!important;visibility:hidden!important;height:0!important;width:0!important;pointer-events:none!important} ' +
        'html, body { background-color: #0f0f0f !important; color-scheme: dark !important; }';
        (document.head || document.documentElement).appendChild(style);
    }
    injectAdCSS();
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', injectAdCSS);
    }

    // ── 6. Ultra-fast video ad fast-forward & skip fallback ──
    function sweepAds() {
        try {
            // Click skip buttons
            var skipBtns = document.querySelectorAll('.ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-skip-ad-button, .ytp-ad-skip-button-slot button, .ytp-ad-text.ytp-ad-skip-button-text');
            for (var b = 0; b < skipBtns.length; b++) {
                if (skipBtns[b] && typeof skipBtns[b].click === 'function') skipBtns[b].click();
            }

            // Close overlays
            var overlays = document.querySelectorAll('.ytp-ad-overlay-close-button, .ytp-ad-overlay-close-container');
            for (var o = 0; o < overlays.length; o++) {
                if (overlays[o] && typeof overlays[o].click === 'function') overlays[o].click();
            }

            // Fast forward playing ads
            var video = document.querySelector('video');
            if (video) {
                var isAdPlaying = document.querySelector('.ad-showing, .ad-interrupting, .ytp-ad-player-overlay');
                if (isAdPlaying || (video.duration && video.duration < 60 && (video.src.indexOf('blob:') === -1 && video.src.indexOf('googlevideo') > -1 && video.parentElement && video.parentElement.classList.contains('ad-container')))) {
                    video.muted = true;
                    video.playbackRate = 16.0;
                    if (isFinite(video.duration) && video.duration > 0 && video.currentTime < video.duration - 0.2) {
                        video.currentTime = video.duration - 0.1;
                    }
                }
            }

            // Dismiss anti-adblock modal if popped
            var enforcement = document.querySelector('ytd-enforcement-message-view-model, tp-yt-paper-dialog:has(ytd-enforcement-message-view-model)');
            if (enforcement) {
                var btn = enforcement.querySelector('button, .yt-spec-button-shape-next');
                if (btn) btn.click();
                enforcement.remove();
            }
        } catch(e) {}
    }
    setInterval(sweepAds, 250);

    // ── 7. SponsorBlock: skip sponsored/intro/outro segments ──
    var sponsorCache = {};
    function skipSponsorSegments(videoId) {
        if (!videoId || sponsorCache[videoId]) return;
        sponsorCache[videoId] = true;
        try {
            var xhr = new XMLHttpRequest();
            xhr.open('GET', 'https://sponsor.ajay.app/api/skipSegments?videoID=' + videoId + '&categories[]=sponsor&categories[]=selfpromo&categories[]=exclusive_access&categories[]=interaction&categories[]=intro&categories[]=outro&categories[]=preview&categories[]=music_offtopic');
            xhr.onload = function() {
                try {
                    var segments = _origParse(xhr.responseText);
                    if (!segments || !Array.isArray(segments) || !segments.length) return;
                    sponsorCache[videoId] = segments;
                    var vid = document.querySelector('video');
                    if (!vid) return;
                    vid.addEventListener('timeupdate', function sbCheck() {
                        for (var i = 0; i < segments.length; i++) {
                            var seg = segments[i].segment;
                            if (seg && seg.length === 2) {
                                var sStart = seg[0], sEnd = seg[1];
                                if (vid.currentTime >= sStart && vid.currentTime < sEnd) {
                                    vid.currentTime = sEnd;
                                    showToast('Sponsor skipped (' + (segments[i].category || 'sponsor') + ')');
                                }
                            }
                        }
                    });
                } catch(e) {}
            };
            xhr.send();
        } catch(e) {}
    }

    function checkVideoUrl() {
        try {
            var vMatch = window.location.href.match(/[?&]v=([a-zA-Z0-9_-]{11})/);
            if (vMatch && vMatch[1]) skipSponsorSegments(vMatch[1]);
        } catch(e) {}
    }
    var _pushState = history.pushState;
    if (_pushState) {
        history.pushState = function() {
            var r = _pushState.apply(this, arguments);
            setTimeout(checkVideoUrl, 800);
            return r;
        };
    }
    window.addEventListener('popstate', function() { setTimeout(checkVideoUrl, 800); });
    setTimeout(checkVideoUrl, 1000);

    // ── 8. Non-intrusive in-app toast notification ──
    var toastEl = null;
    function showToast(msg) {
        try {
            if (!toastEl) {
                toastEl = document.createElement('div');
                toastEl.id = '_bt_toast';
                toastEl.style.cssText = 'position:fixed;bottom:24px;right:24px;background:rgba(18,18,24,0.92);backdrop-filter:blur(10px);border:1px solid rgba(255,255,255,0.12);box-shadow:0 8px 32px rgba(0,0,0,0.5);border-radius:8px;padding:10px 18px;font-size:12.5px;color:#fff;z-index:999999;opacity:0;transition:opacity 0.25s cubic-bezier(0.16,1,0.3,1),transform 0.25s cubic-bezier(0.16,1,0.3,1);transform:translateY(10px);pointer-events:none;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;font-weight:500';
                document.documentElement.appendChild(toastEl);
            }
            toastEl.textContent = msg;
            toastEl.style.opacity = '1';
            toastEl.style.transform = 'translateY(0)';
            setTimeout(function() {
                if (toastEl) {
                    toastEl.style.opacity = '0';
                    toastEl.style.transform = 'translateY(10px)';
                }
            }, 2500);
        } catch(e) {}
    }
    window.__blackTubeShowToast = showToast;
})();
";

        public static async Task AttachAdShieldAsync(WebView2 webView)
        {
            if (webView == null || webView.CoreWebView2 == null) return;

            // 1. Add Network Request Filters
            foreach (string domain in AdDomains)
            {
                webView.CoreWebView2.AddWebResourceRequestedFilter(
                    "*" + domain + "*", CoreWebView2WebResourceContext.All);
            }

            webView.CoreWebView2.WebResourceRequested += (sender, args) =>
            {
                try
                {
                    string uri = args.Request.Uri.ToLowerInvariant();

                    if (args.ResourceContext == CoreWebView2WebResourceContext.Document)
                        return;

                    foreach (string streamDomain in AllowedStreamDomains)
                    {
                        if (uri.Contains(streamDomain))
                            return;
                    }

                    foreach (string domain in AdDomains)
                    {
                        if (uri.Contains(domain))
                        {
                            args.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                new MemoryStream(new byte[0]), 200, "OK", "Content-Type: text/plain");
                            break;
                        }
                    }
                }
                catch { }
            };

            // 2. Add Script To Document Created (Runs before any web page scripts)
            try
            {
                await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(CoreAdShieldScript);
            }
            catch { }
        }
    }
}
