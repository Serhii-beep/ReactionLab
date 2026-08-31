import { TestBed } from '@angular/core/testing';
import { Theme } from './theme';

describe('Theme', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
  });

  it('defaults to system', () => {
    expect(TestBed.inject(Theme).preference()).toBe('system');
  });

  it('applies the chosen theme to the document and persists it', () => {
    const theme = TestBed.inject(Theme);

    theme.prefer('dark');
    TestBed.tick();

    expect(theme.resolved()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(localStorage.getItem('reactionlab.theme')).toBe('dark');
  });

  it('restores a stored preference', () => {
    localStorage.setItem('reactionlab.theme', 'light');
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});

    expect(TestBed.inject(Theme).preference()).toBe('light');
  });
});
