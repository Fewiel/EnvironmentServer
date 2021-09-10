declare class DarkMode {
    static readonly DATA_KEY = "bs.prefers-color-scheme";
    static readonly DATA_SELECTOR = "bs-color-scheme";
    static readonly VALUE_LIGHT = "light";
    static readonly VALUE_DARK = "dark";
    static readonly CLASS_NAME_LIGHT = "light";
    static readonly CLASS_NAME_DARK = "dark";
    get inDarkMode(): boolean;
    set inDarkMode(val: boolean);
    private _hasGDPRConsent;
    get hasGDPRConsent(): boolean;
    set hasGDPRConsent(val: boolean);
    cookieExpiry: number;
    get documentRoot(): HTMLHtmlElement;
    constructor();
    static saveCookie(name: string, value?: string, days?: number): void;
    private saveValue;
    static readCookie(name: string): string;
    readValue(name: string): string;
    eraseValue(name: string): void;
    getSavedColorScheme(): string;
    getPreferedColorScheme(): string;
    setDarkMode(darkMode: boolean, doSave?: boolean): void;
    toggleDarkMode(doSave?: boolean): void;
    resetDarkMode(): void;
    static getColorScheme(): string;
    static updatePreferedColorSchemeEvent(): void;
    static onDOMContentLoaded(): void;
}
declare const darkmode: DarkMode;
//# sourceMappingURL=darkmode.d.ts.map