const APPLE = /Mac|iPhone|iPad/;

export function isApplePlatform(): boolean {
    return APPLE.test(navigator.userAgent);
}