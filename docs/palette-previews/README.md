# Palette comparisons

Full-page captures of the landing page under every colour scheme that was
considered, so the choice can be re-examined without re-running the exercise.

`lean-landing-applied.jpg` is the scheme in production: **Petrol & Graphite**,
a petrol teal accent (`#0f7989`) on neutral graphite (`#25333f`).

## How these were made

Each image is a capture of the running site, not a mock-up. For every scheme the
values in `frontend/lean-portal/src/styles/_tokens.scss` were replaced, the app
rebuilt, and the page captured through headless Chrome at 1440px wide with
reduced motion forced — so all of them show the same hero slide and the same
figures, and only the colour differs.

That every scheme is a change to that one file, with nothing else touched, is
what the exercise demonstrates.

## `cool/` — the corporate set

| file | accent | ground | accent on white |
|---|---|---|---|
| `lean-landing-enterprise.jpg` | `#1263bd` | `#1b3a63` | 5.92:1 |
| `lean-landing-cobalt.jpg` | `#1f5fd0` | `#3a4654` | 5.82:1 |
| `lean-landing-petrol.jpg` | `#0f7a8a` | `#25333f` | 5.03:1 |
| `lean-landing-teal.jpg` | `#10786e` | `#173c40` | 5.34:1 |
| `lean-landing-indigo.jpg` | `#4351c9` | `#262d54` | 6.44:1 |
| `lean-landing-burgundy.jpg` | `#9e2749` | `#1e3252` | 7.39:1 |

Petrol was chosen from this set. The shipped accent is `#0f7989`, a shade darker
than the `#0f7a8a` previewed here, because the preview value cleared 4.5:1 on
white but landed at 4.48:1 on `--c-surface-sunken`, which links also sit on.

## `warm/` — the first set, not taken forward

`lean-landing-current.jpg` is the coral scheme that preceded this one; the rest
are saffron, ashoka blue, terracotta, crimson and bronze.

The warm schemes in this set read as muddy, and the reason is measurable rather
than a matter of taste. Any hue has to be darkened until it clears 4.5:1 against
white, and warm hues have much further to fall before they get there:

| hue | lightness where it clears AA |
|---|---|
| saffron / orange | 39% |
| amber | 34% |
| olive-gold | 29% |
| azure | 48% |
| royal blue | 60% |
| indigo | 65% |

Saffron and bronze had to be pushed to 34–39% lightness, which is where orange
stops reading as orange and starts reading as brown. Cool accents clear the same
bar 10–25 points higher and keep their chroma. That is why the shipped scheme
has a cool accent.
