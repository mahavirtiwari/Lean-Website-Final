# Editor guide

How to run the MSME Competitive (LEAN) Scheme portal from the admin console.
Written for the people maintaining the content, not for developers.

---

## Signing in

Go to `/admin`. Use the account issued to you by the Ministry.

The first time you sign in — or after an administrator resets your password —
the console asks you to set your own password before anything else opens. It
must be at least 12 characters, with upper and lower case letters, a digit and
a symbol. Changing it signs out your other sessions.

Every action you take is recorded against your name in the activity log.

### What your role lets you do

| Role | Can do |
|---|---|
| **Editor** | Create and edit content, save drafts, submit for review |
| **Publisher** | Everything an editor can, plus publish, unpublish and delete |
| **Administrator** | Everything, plus manage users, roles and site settings |
| **Viewer** | Read-only access to the console |

If a button you expect is missing, your role does not permit that action.

---

## The publishing workflow

Content moves through four states:

**Draft** → **In review** → **Published** → **Archived**

- A new page or notice always starts as a **draft**. Drafts are invisible to the
  public.
- An editor saves work and submits it for review.
- A publisher opens it, checks it, and presses **Publish**. It appears on the
  site immediately.
- **Unpublish** returns it to draft — useful when something needs correcting
  quickly.

Saving and publishing are separate. **Save** stores your changes; it does not
put them live. On an already-published page, however, saving *does* update the
live page — so review before you save on live content.

---

## Pages

**Content → Pages.**

### Creating a page

1. **New page**.
2. Enter the **title**. The **slug** is filled in for you when you leave the
   title field.
3. Choose a **parent page** if it belongs in a section. This drives the
   breadcrumb trail and the left-hand navigation on the published page.
4. Write the content, then **Save**.

### About the slug

The slug is the page's address. `about-scheme/objective` publishes to
`/about-scheme/objective`.

Once a page is published, **avoid changing its slug**. Anyone who bookmarked or
linked to the old address will get a "page not found". If you must change it,
tell whoever maintains the links.

### Templates

| Template | Use for |
|---|---|
| **Sidebar left** | Standard internal pages. Shows section navigation and support widgets in the left rail. This is the right choice for almost everything. |
| **Full width** | A page that should fill the column with no sidebar. |
| **Composed blocks** | The home page. Built from bands rather than a single body. |
| **Custom component** | Pages with their own screen — gallery, FAQs, contact. Do not change this unless a developer asks you to. |

### Writing content

The content field accepts HTML. The styles are applied for you, so you only need
the structure:

```html
<h3>A section heading</h3>
<p>A paragraph of text.</p>

<ul>
  <li>A bullet point</li>
</ul>

<ol class="step-list">
  <li><strong>Step one</strong> — numbered steps in a circle.</li>
</ol>

<div class="callout callout--info">
  <p>A highlighted note.</p>
</div>

<table class="data-table">
  <thead><tr><th scope="col">Level</th><th scope="col">Cost</th></tr></thead>
  <tbody><tr><td>Basic</td><td>Free of cost</td></tr></tbody>
</table>
```

Use `callout--info`, `callout--success`, `callout--warning` or `callout--danger`
for highlighted notes.

Anything unsafe — scripts, event handlers, unknown embeds — is stripped when you
save. Only YouTube, Vimeo and Google Maps embeds are kept. This is deliberate
and cannot be overridden.

### SEO and banner

The **SEO & banner** tab controls how the page appears in search results and
when shared. Leave the meta title blank to use the page title. Aim for 150–160
characters in the meta description.

---

## The home page

**Content → Pages → Home**, then the **Page blocks** tab.

The landing page is built from bands. You control which appear, in what order,
and what each says.

### What you can change

- **Re-order** — the up and down arrows. The order in the list is the order down
  the page.
- **Hide** — the eye icon. The band keeps its settings but stops rendering.
  Prefer this to deleting when something is only temporarily not needed.
- **Edit** — heading, eyebrow, sub-heading, body, image and buttons.
- **Add** or **Remove** a band.

### Where each band gets its content

Some bands carry their own text. Others pull from a dedicated screen, and
editing the band only changes its heading:

| Band | Content comes from |
|---|---|
| Hero carousel | **Home page → Banners** |
| Quick action cards | The block's own settings (JSON) |
| Welcome and video | The block itself |
| Statistics counters | **Home page → Statistics** |
| Scheme components | **Scheme → Components** |
| Documents and notices | **Content → Documents** (featured) and **News & notices** |
| Ministry message | The block itself |
| Scheme levels | **Scheme → Scheme levels** |
| Login portals | **Home page → Login portals** |
| Initiatives | The block's own settings (JSON) |
| Success stories | **Home page → Success stories** |
| Useful links | **Scheme → Partners**, type "Useful link" |
| Call to action, Contact strip | The block itself |

### The settings JSON

Two bands — quick actions and initiatives — hold their cards in a JSON field.
The shape is:

```json
{
  "cards": [
    {
      "title": "Scheme Guideline",
      "description": "Explore the approved LEAN guidelines.",
      "icon": "file-text",
      "url": "/downloads",
      "linkText": "Download"
    }
  ]
}
```

Edit the text between the quotation marks. Keep the punctuation — a missing
comma or bracket means the band falls back to empty. If that happens, the page
still renders; fix the JSON and save again.

---

## News and notices

**Content → News & notices.**

Choose the right **type** — it drives where the item appears and how it is
labelled:

| Type | Use for |
|---|---|
| Announcement | Scheme announcements |
| News | General news |
| Circular | Official circulars — attach the PDF |
| Tender | Tender notices |
| Press release | Ministry releases |

**Featured** promotes the item to the top of the listing. **Show in ticker**
puts it in the scrolling strip at the top of the home page — use it sparingly,
three or four items at most.

Set an **expiry date** on anything time-bound, such as a tender. The item hides
itself automatically on that date; you do not need to remember to remove it.

---

## Documents

**Content → Documents.**

1. Upload the file in the **Media library** first.
2. Copy its URL.
3. Create the document record and paste the URL in.

Fill in the **file size** — it is shown next to the download link so people on
slow connections know what they are getting. **Featured** documents appear in
the "Documents & notices" band on the home page.

Downloads are counted automatically.

---

## Gallery

**Content → Gallery.**

Create an **album**, then add images to it. Upload the images in the media
library first and paste their URLs.

**Alternative text is required on every image.** It is what a person using a
screen reader hears in place of the photograph, and the form will not save
without it. Describe what is in the picture:

- Good: "MSME representatives at the LEAN awareness workshop in Pune"
- Not useful: "image1", "photo", "workshop"

---

## Media library

**Content → Media library.**

Upload images and documents here, then use the link icon to copy a file's URL
for use elsewhere in the CMS.

| Type | Limit |
|---|---|
| Images — JPG, PNG, GIF, WebP, SVG | 5 MB |
| Documents — PDF, Word, Excel, PowerPoint, CSV, ZIP | 25 MB |

Compress large photographs before uploading. A 4 MB photograph on the home page
is a slow page for someone on a mobile connection.

**Deleting a file is permanent**, and any page still pointing at it will show a
broken image or a dead link. Check before you delete.

---

## Navigation

**Site → Navigation.**

Five menus:

| Menu | Where it appears |
|---|---|
| **Main menu** | The primary navigation bar. Two levels. |
| **Utility bar** | Small links in the strip at the very top |
| **Quick links** | Footer column of shortcuts |
| **Useful links** | Footer column of external services |
| **Footer policies** | Policy links along the bottom |

Each item links to either a **CMS page** or an explicit **URL**. Choosing a page
is better — the link follows if the page is renamed.

Tick **Open in a new tab** for external links, and **Render as a highlighted
button** for a call to action such as "Register for LEAN Scheme".

Deleting a parent with sub-items is blocked. Move or delete the sub-items first.

---

## Enquiries

**Site → Enquiries.**

Messages from the public contact form. Each gets a reference number
(`LEAN-ENQ-000123`) that the sender is shown.

Work through them with the status column:

**New** → **In progress** → **Responded** → **Closed**

Use **Spam** for junk rather than deleting, so the pattern stays visible.

**Reply by e-mail** opens your mail client with the reference already in the
subject line. Record what you did in the **internal notes** — those are visible
only to portal staff.

**Export CSV** gives you the filtered list for reporting.

---

## Site settings

**Site → Settings.** Administrators only.

Grouped into tabs. Save each tab separately — the console only submits the tab
you are looking at, so you cannot accidentally overwrite values on another.

| Tab | Holds |
|---|---|
| General | Portal name, ministry, logos |
| Contact | Address, helpline, e-mail, working hours |
| Social | Social media links |
| External links | Udyam, ZED, LMS and other portals |
| Application | Deep links into the transactional LEAN system |
| Features | Toggles for the ticker, newsletter and success stories |
| SEO | Default title, description and keywords |
| Footer | Copyright, disclaimer, last-updated date |

Take care in **Application** — those links carry people into the registration
system. Test any change.

---

## Users

**Site → Users.** Administrators only.

Give people the **least role that lets them do their job**. Most content staff
need Editor. Reserve Publisher for those responsible for what goes live, and
Administrator for one or two people.

When someone leaves, **deactivate** rather than delete. The account is disabled
and their session ends immediately, but the activity log keeps its record of
what they did.

Resetting a password gives you a value to pass to the person through a secure
channel — not e-mail. They must change it at next sign-in.

---

## Accessibility, briefly

This is a government portal and must remain usable by everyone.

- **Always write alternative text** for images. It is required on banners and
  gallery images and should be filled in everywhere else too.
- **Use headings in order** — `<h3>` then `<h4>`. Do not pick a heading level
  because of how big it looks.
- **Write meaningful link text.** "Download the scheme guidelines", not "click
  here".
- **Do not rely on colour alone** to carry meaning.
- **Keep tables simple**, with a proper header row.

---

## If something goes wrong

| Problem | What to do |
|---|---|
| A change is not on the live site | Check the status. A draft is not published. |
| "Slug already in use" | Another page has that address. Choose a different one. |
| A page will not delete | It has child pages or a menu item points at it. Deal with those first. |
| An image is broken | The file was deleted from the media library, or the URL is wrong. |
| Signed out unexpectedly | Sessions end when the browser tab closes, and after a password change. |
| A band vanished from the home page | Check whether it is hidden rather than removed — the eye icon. |

For anything else, contact the portal administrator. The activity log records
every change, so it is usually possible to see what happened and when.
