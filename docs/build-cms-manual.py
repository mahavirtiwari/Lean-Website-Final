"""
Builds the CMS user manual as a Word document.

Content is taken from the console as it is actually built - the navigation
groups, the screen descriptions and field lists in resource-definitions.ts, the
settings tabs in the database, and the order of the home page bands returned by
the API - so the manual describes the portal that exists rather than the one
somebody remembers.
"""
from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Inches, Pt, RGBColor
import os

TEAL = RGBColor(0x0F, 0x79, 0x89)
GREY = RGBColor(0x66, 0x6F, 0x7C)

doc = Document()

# ----------------------------------------------------------------- styling ----
normal = doc.styles['Normal']
normal.font.name = 'Calibri'
normal.font.size = Pt(10.5)
normal.paragraph_format.space_after = Pt(6)

for name, size in (('Heading 1', 17), ('Heading 2', 13), ('Heading 3', 11)):
    st = doc.styles[name]
    st.font.name = 'Calibri'
    st.font.size = Pt(size)
    st.font.color.rgb = TEAL
    st.font.bold = True

for section in doc.sections:
    section.top_margin = Inches(0.8)
    section.bottom_margin = Inches(0.8)
    section.left_margin = Inches(0.9)
    section.right_margin = Inches(0.9)



IMAGES = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'manual-images')

# Screen name -> (the console screen, the part of the site it changes).
SHOTS = {
    'Pages': ('cms-pages.png', None),
    'News & notices': ('cms-posts.png', 'site-documents.png'),
    'Documents': ('cms-documents.png', 'site-downloads.png'),
    'Gallery': ('cms-gallery.png', 'site-gallery.png'),
    'Media library': ('cms-media.png', None),
    'Banners': ('cms-banners.png', None),
    'Statistics': ('cms-statistics.png', 'site-statistics.png'),
    'Login portals': ('cms-login-portals.png', None),
    'Benefits': ('cms-benefits.png', 'site-benefits.png'),
    'Incentives': ('cms-incentives.png', 'site-incentives.png'),
    'Success stories': ('cms-success-stories.png', 'site-success-stories.png'),
    'Scheme levels': ('cms-scheme-levels.png', None),
    'Components': ('cms-components.png', 'site-components.png'),
    'Programmes': ('cms-programmes.png', None),
    'Partners': ('cms-partners.png', 'site-partners.png'),
    'FAQs': ('cms-faqs.png', 'site-faqs.png'),
    'Navigation': ('cms-navigation.png', 'site-footer.png'),
    'Branding': ('cms-branding.png', None),
    'Enquiry mail': ('cms-enquiry-mail.png', None),
    'Helpdesk (Zoho)': ('cms-helpdesk.png', 'site-contact-qci.png'),
    'Integrations': ('cms-integrations.png', None),
    'Settings': ('cms-settings.png', None),
    'Users': ('cms-users.png', None),
    'Activity log': ('cms-activity.png', None),
}


def shot(filename, caption):
    """A screenshot with its caption, skipped silently when not captured."""
    if not filename:
        return
    path = os.path.join(IMAGES, filename)
    if not os.path.exists(path):
        return

    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(6)
    p.paragraph_format.space_after = Pt(2)
    p.add_run().add_picture(path, width=Inches(6.3))

    cap = doc.add_paragraph()
    cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = cap.add_run(caption)
    run.italic = True
    run.font.size = Pt(8.5)
    run.font.color.rgb = GREY
    cap.paragraph_format.space_after = Pt(10)


def para(text, *, italic=False, bold=False, colour=None, size=None, after=6):
    p = doc.add_paragraph()
    run = p.add_run(text)
    run.italic = italic
    run.bold = bold
    if colour:
        run.font.color.rgb = colour
    if size:
        run.font.size = Pt(size)
    p.paragraph_format.space_after = Pt(after)
    return p


def bullets(items):
    for item in items:
        p = doc.add_paragraph(item, style='List Bullet')
        p.paragraph_format.space_after = Pt(3)


def table(headers, rows, widths=None):
    t = doc.add_table(rows=1, cols=len(headers))
    t.style = 'Light Grid Accent 1'
    t.alignment = WD_TABLE_ALIGNMENT.CENTER
    for i, h in enumerate(headers):
        cell = t.rows[0].cells[i]
        cell.text = ''
        run = cell.paragraphs[0].add_run(h)
        run.bold = True
        run.font.size = Pt(9.5)
    for row in rows:
        cells = t.add_row().cells
        for i, value in enumerate(row):
            cells[i].text = ''
            run = cells[i].paragraphs[0].add_run(str(value))
            run.font.size = Pt(9.5)
    if widths:
        for row in t.rows:
            for i, w in enumerate(widths):
                row.cells[i].width = Inches(w)
    doc.add_paragraph().paragraph_format.space_after = Pt(4)
    return t


# ------------------------------------------------------------------ cover ----
title = doc.add_paragraph()
title.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = title.add_run('MSME Competitive (LEAN) Scheme Portal')
run.bold = True
run.font.size = Pt(22)
run.font.color.rgb = TEAL

sub = doc.add_paragraph()
sub.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = sub.add_run('Content Management System — User Manual')
run.font.size = Pt(14)
run.font.color.rgb = GREY

meta = doc.add_paragraph()
meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = meta.add_run('Ministry of Micro, Small and Medium Enterprises, Government of India\n'
                   'How to change each part of the website from the admin console')
run.font.size = Pt(10)
run.font.color.rgb = GREY

doc.add_paragraph()

# --------------------------------------------------------------- section 1 ----
doc.add_heading('1. Before you start', level=1)

doc.add_heading('Signing in', level=2)
para('Open /admin and sign in with the account issued to you. The first time you sign in, and '
     'after any password reset, the console asks you to set your own password before anything '
     'else will open. This is enforced by the server, not just the screen, so the rest of the '
     'console stays closed until it is done.')
bullets([
    'Verification. Below the password, type the five characters shown in the picture — capital '
    'or small letters both work. If you cannot read them, press the refresh button for new ones, '
    'or choose "Can\'t read the characters? Answer a question instead" for a short sum that a '
    'screen reader can read out. Each challenge can be used once; after a failed attempt a new '
    'one is shown.',
    'Five wrong passwords in a row lock the account for 15 minutes. Wait, then sign in again, '
    'or ask an administrator to reset the password under Users.',
    'Signed out after 30 minutes. If you leave the console without touching the mouse or keyboard '
    'for 30 minutes, it signs you out and the sign-in page says so. Save your work before leaving '
    'the desk; anything unsaved is lost.',
])
shot('cms-login.png', 'The sign-in page, with the verification challenge')

doc.add_heading('What your role allows', level=2)
table(['Role', 'What it permits'], [
    ['Editor', 'Create and edit content, save drafts, submit for review'],
    ['Publisher', 'Everything an editor can do, plus publish, unpublish and delete'],
    ['Administrator', 'Everything, plus users, site settings and mail'],
    ['Viewer', 'Read only'],
], widths=[1.5, 5.0])
para('If a button you expect is missing, your role does not permit that action. Every change is '
     'recorded against your name in the Activity log.', italic=True)

doc.add_heading('Five rules that apply everywhere', level=2)
para('These behave the same way on every screen. Knowing them saves most of the questions asked '
     'about the console.')
bullets([
    'Changes are live immediately. When you save, the public site is updated at once — there is '
    'no publish step for a saved change and no cache to wait for.',
    'Disable hides, Remove deletes. Use Disable to take something off the site while keeping it. '
    'Removed items cannot be restored from the console.',
    'Order is what you see. The arrows, or the Order field, set the order items appear on the '
    'site. Smaller numbers come first.',
    'Emptying a text field removes what it labels. Clearing a heading in Settings does not leave '
    'a blank space — the heading, and often the whole band, stops being drawn.',
    'Images go through the Media library. Upload there first, copy the URL, then paste it into '
    'the field that needs it. Fields with an Upload button do this for you.',
])

doc.add_page_break()

# --------------------------------------------------------------- section 2 ----
doc.add_heading('2. The home page, band by band', level=1)
para('The home page is assembled from bands in the order below. Each band is drawn from a '
     'different screen in the console, so this table is the quickest way to find where something '
     'on the home page comes from.')

table(['#', 'Band on the home page', 'Edited in', 'Wording edited in'], [
    ['1', 'Hero carousel', 'Banners', 'Banners (per slide)'],
    ['2', 'Start here — quick action cards', 'Pages → Home → the band', 'The band itself'],
    ['3', 'Welcome / LEAN toolkit', 'Pages → Home → the band', 'The band itself'],
    ['4', 'Registration & login tiles', 'Login portals', 'The band heading'],
    ['5', 'The scheme in numbers', 'Statistics', 'The band heading'],
    ['6', 'Benefits / Incentives cards', 'Benefits', 'The band heading'],
    ['7', 'Six components of the scheme', 'Components', 'The band heading'],
    ['8', 'Documents & notices', 'Documents and News & notices', 'The band heading'],
    ['9', "Minister's message", 'Pages → Home → the band', 'The band itself'],
    ['10', 'LEAN initiatives', 'Pages → Home → the band', 'The band itself'],
    ['11', 'Success stories carousel', 'Success stories', 'The band heading'],
    ['12', 'Our partners strip', 'Partners', 'The band heading'],
    ['13', 'Gallery — pictures and film', 'Gallery', 'The band heading'],
    ['14', 'Ready to make your enterprise LEAN?', 'Pages → Home → the band', 'The band itself'],
], widths=[0.4, 2.6, 2.1, 1.9])

para('To reorder, retitle or hide a band, open Pages → Home. Each band has its own eyebrow, '
     'heading, sub-heading and a visibility switch. Hiding a band there hides it whatever the '
     'screen behind it contains.')

doc.add_page_break()

# --------------------------------------------------------------- section 3 ----
doc.add_heading('3. Each console screen, and what it changes', level=1)


def screen(name, group, controls, appears, fields, notes=None):
    # The group heading above already says which part of the console this is in.
    doc.add_heading(f'{name}', level=3)
    para(controls)

    p = doc.add_paragraph()
    run = p.add_run('Where it appears:  ')
    run.bold = True
    run.font.size = Pt(9.5)
    run = p.add_run(appears)
    run.font.size = Pt(9.5)
    p.paragraph_format.space_after = Pt(4)

    p = doc.add_paragraph()
    run = p.add_run('Main fields:  ')
    run.bold = True
    run.font.size = Pt(9.5)
    run = p.add_run(fields)
    run.font.size = Pt(9.5)
    p.paragraph_format.space_after = Pt(4)

    if notes:
        for note in notes:
            p = doc.add_paragraph(note, style='List Bullet')
            p.paragraph_format.space_after = Pt(2)
            for r in p.runs:
                r.font.size = Pt(9.5)

    cms_shot, site_shot = SHOTS.get(name, (None, None))
    shot(cms_shot, f'Where it is edited — {name} in the console')
    shot(site_shot, 'Where it appears on the website')

    doc.add_paragraph().paragraph_format.space_after = Pt(2)


doc.add_heading('Content', level=2)

screen('Pages', 'Content',
       'Every page on the site except the home page. The page address, its wording, its banner '
       'and how it is laid out.',
       'At its own address, for example /about-scheme/introduction. Also in the left-hand menu '
       'of the section it belongs to, and in the sitemap.',
       'Title, Slug (the address), Summary, Body, Banner image and caption, Template, Status, '
       'Order, Parent page, Meta title and description',
       ['Template decides the layout: Sidebar left is the usual one; Full width drops the side '
        'rail; Custom hands the page to a built-in screen such as Downloads or Contact.',
        'Status must be Published for the page to be visible to the public.',
        'Changing the slug changes the address. Anything linking to the old one will break.'])

screen('News & notices', 'Content',
       'Announcements, circulars, tenders and press releases.',
       'The News & announcements page at /media/news, the Documents & notices band on the home '
       'page, and the scrolling ticker under the masthead.',
       'Type, Title, Slug, Excerpt, Body, Cover image, Attachment, Featured, Status, Published date',
       ['Featured items are the ones pulled onto the home page.',
        'The ticker shows the most recent notices; it is switched on under Settings → Features.'])

screen('Documents', 'Content',
       'Guidelines, brochures, circulars, formats and presentations offered for download.',
       'The Downloads page, the Documents & notices band on the home page, and the Key documents '
       'panel in the side rail of internal pages.',
       'Title, Category, Description, File URL, File type and size, Language, Version, Document '
       'date, Featured, Status, Order',
       ['Upload the file in Media library first, then paste its URL here.',
        'Category drives the filter buttons on the Downloads page.'])

screen('Gallery', 'Content',
       'Photo and film albums from programmes, workshops and factory visits.',
       'The Gallery page at /gallery, and the pictures and film rails on the home page.',
       'Album: Title, Description, Cover image, Date, Order, Active. Inside an album: images, '
       'alternative text, video URL, Order, Active',
       ['A video is added by putting its YouTube or Vimeo address in the item’s video field.',
        'The home page shows the first few and then scrolls; the Gallery page shows everything.'])

screen('Media library', 'Content',
       'Every image and file uploaded to the portal.',
       'Nothing directly. It is the store other screens draw from.',
       'File, Alternative text, Folder',
       ['Copy an item’s URL to paste into any field that asks for an image or a file.',
        'Give images meaningful alternative text — it is what a screen reader announces.'])

doc.add_heading('Home page', level=2)

screen('Banners', 'Home page',
       'The slides in the hero carousel at the top of the home page.',
       'Band 1 of the home page.',
       'Eyebrow, Title, Highlighted title, Subtitle, Image, Mobile image, Alternative text, two '
       'buttons, Order, Active, Starts at, Ends at',
       ['Alternative text is required. It is what a screen reader announces in place of the image.',
        'Starts at and Ends at schedule a slide; leave them empty for a slide that always shows.',
        'Highlighted title is the part drawn in the accent colour.'])

screen('Statistics', 'Home page',
       'The counter strip — the scheme in numbers.',
       'Band 5 of the home page.',
       'Label, Value, Prefix, Suffix, Icon, Link, Source key, Order, Active',
       ['Where a source key is set, the value is refreshed automatically from the transactional '
        'LEAN database and what you type is ignored.'])

screen('Login portals', 'Home page',
       'The stakeholder sign-in and registration tiles.',
       'Band 4 of the home page.',
       'Title, Audience, Description, Icon, Accent colour, Login URL and text, Register URL and '
       'text, Order, Active, Opens in a new tab',
       ['A relative address such as /VerifyUdyam/Register is resolved against the transactional '
        'LEAN application, whose base address is set under Settings → Application.'])

screen('Benefits', 'Home page',
       'The cards in the Benefits / Incentives band. Each names a source of support and opens '
       'the page listing what it offers.',
       'Band 6 of the home page.',
       'Title, Description, Links to, Button text, Opens in a new tab, Artwork or Icon, Order, Active',
       ['Three cards spread to fill the row; you are not restricted to four.',
        'Artwork replaces the icon when uploaded.'])

screen('Incentives', 'Home page',
       'The individual incentives listed on the four Benefits / Incentives pages.',
       'The listing pages under /benefits-incentives — Ministry of MSME, States / UTs, Financial '
       'Institutions and Other.',
       'Category, Title, Description, Scheme level, State / UT, Offered by and logo, Contact name, '
       'e-mail and telephone, two documents, Film, Where to claim it, Order, Active',
       ['Category decides which of the four pages it appears on.',
        'Leave Scheme level empty when it applies at every level — it then stays in the list '
        'whichever level a visitor filters by.',
        'The level and state filters appear on a page only when its entries carry those values.',
        'A film is shown as a Watch film button that opens a player over the page.'])

screen('Success stories', 'Home page',
       'MSME success stories.',
       'Band 11 of the home page, as a carousel that scrolls on its own.',
       'Unit name, Person, Designation, Location, Sector, Level achieved, Quote, Impact, '
       'Photograph, Video, Status, Order')

doc.add_heading('Scheme', level=2)

screen('Scheme levels', 'Scheme',
       'The LEAN Pledge and the Bronze, Silver and Gold implementation tiers.',
       'The Scheme Levels page, and wherever a level is named across the site.',
       'Level, Name, Badge, Tagline, Description, Deliverables, Fees, Duration, Certificate image, '
       'Icon, Accent colour, Order, Active',
       ['Deliverables are entered one per line and drawn as a tick list.'])

screen('Components', 'Scheme',
       'The six components of the scheme.',
       'Band 7 of the home page, as an icon grid, and the Scheme Components page.',
       'Title, Icon, Short description, Description, Link, Order, Active')

screen('Programmes', 'Scheme',
       'Awareness programmes, and training for assessors and consultants.',
       'The Awareness Programmes and Training pages under /programmes.',
       'Code, Type, Title, Agency, Description, State, District, Venue, Start and end dates, '
       'Registered count, Capacity, Registration URL, Programme status, Status')

screen('Partners', 'Scheme',
       'Implementation agencies, industry associations, OEMs, consultant organisations and the '
       'Useful Links tiles.',
       'Band 12 of the home page (the partners strip), the Useful Links column in the footer, '
       'the Implementation Agency page, and the agency choice on the Contact page.',
       'Type, Name, Short name, Description, Website, Contact page, E-mail, Enquiries inbox, Own '
       'enquiry form, Telephone, Address, Logo, Order, Active, Featured',
       ['Type decides where a record appears. Useful Link puts it in the footer column; '
        'Implementation Agency puts it on the Contact page and in the partners strip.',
        'Enquiries inbox is where enquiries choosing that agency are e-mailed. It is never shown '
        'on the site — see section 5.',
        'Own enquiry form is optional: a Zoho (or other) form address shown instead of the '
        'portal’s own form once that agency is chosen.'])

screen('FAQs', 'Scheme',
       'The questions shown in the accordion on the FAQs page.',
       'The FAQs page, grouped by topic. The ask-a-question assistant also searches them.',
       'Question, Answer, Category, Featured, Status, Order')

doc.add_heading('Site', level=2)

screen('Navigation', 'Site',
       'Every menu on the public site.',
       'The main menu in the masthead, the Website Policies and Quick Links columns in the '
       'footer, the Useful Links column, and the links in the bottom bar of every page.',
       'Label, Links to a page or an explicit address, Parent, Order, Opens in a new tab, '
       'Highlighted, Active',
       ['An item can point at a CMS page or at any address, including an external one.',
        'Disabling an item hides it without losing it — several links are disabled by default.',
        'Footer bottom bar holds the links in the dark strip at the very bottom, after the '
        'ownership line: Disclaimer, Sitemap and Screen Reader Access. Footer policies is the '
        'Website Policies column above it. To move a link between the two, add it to one and '
        'remove it from the other.'])

screen('Branding', 'Site',
       'The logos in the masthead and the footer, where each one links to, and the colour theme '
       'of the whole site.',
       'Every page: the header logos top left, the footer logos, and the colour of buttons, '
       'links, headings, the menu bar and the footer.',
       'Four logo slots (Header left, Header right, Footer, Second footer) — each with an image, '
       'the address it opens, whether it opens in a new tab, and a description for screen '
       'readers. Colour theme, or your own accent and dark colours.',
       ['Upload a logo with the Upload button beside the slot, or paste the address of one '
        'already in the Media library. Remove takes an optional logo off the page; Back to the '
        'original restores the logo the portal was delivered with.',
        'The footer is dark, so the footer logos should be light or white versions.',
        'Six themes are offered: Ministry Blue, Forest Green, Heritage Maroon, Royal Purple, '
        'Saffron & Charcoal, and Indigo & Slate. Choose Custom to set the two colours yourself; '
        'the console refuses a colour too light for white text on it to be read.',
        'A change of theme shows on the public site as soon as it is saved. A visitor who already '
        'has the site open sees it on their next page load.',
        'The State Emblem is part of the Ministry lockup and must not be altered or recoloured.'])

screen('Enquiry mail', 'Site',
       'The mail server enquiries are sent through, and where each implementing agency\'s '
       'enquiries go.',
       'Nothing on the public site. Enquiries are e-mailed; they are not listed in the console.',
       'The portal\'s mail server (server, port, encryption, sign-in name, password, send-as '
       'address, sender name), a blind copy address, and for each agency: its Enquiries inbox '
       'and, optionally, a mail server of its own',
       ['"Use Outlook / Microsoft 365 settings" fills in the server, port and encryption for you.',
        'Under each agency, the switch reads "Sent through the portal\'s mail server". Turn it '
        'on — it then reads "Sent from its own mail server" — for an agency, NPC say, whose '
        'enquiries should leave from its own mailbox, and fill in that server\'s details.',
        'An agency whose enquiries are raised as tickets in Zoho Desk (QCI) is marked as such; '
        'its inbox is used when Zoho Desk is switched off, or when a ticket could not be raised '
        'after repeated attempts.',
        'Send a test message from each block before relying on it. See section 5.'])

screen('Helpdesk (Zoho)', 'Site',
       'Raises QCI\'s enquiries as tickets in Zoho Desk, and the lists a visitor chooses from '
       'to classify an enquiry to QCI (the grievance matrix).',
       'The Contact Us page: once a visitor chooses QCI, the form asks the category questions '
       'from the grievance matrix. The ticket then appears in QCI\'s Zoho Desk.',
       'Connection: Enquiries for (the agency), Zoho account (India, .in), Accounts server, Desk '
       'API, Organisation id, Department id, Client id, Client secret, Refresh token, Channel, '
       'Contact owner id. Ticket fields. Grievance matrix and the names of its levels.',
       ['Only QCI is connected. NPC\'s enquiries go by e-mail, through Enquiry mail.',
        'The secret and the refresh token are stored encrypted and never shown again. Leave the '
        'fields empty when saving to keep them; type new ones to replace them.',
        '"Test the saved connection" signs in to Zoho and reports whether it worked, without '
        'raising a ticket.',
        'If Zoho cannot be reached, the enquiry is kept and the portal tries again on a schedule; '
        'the number waiting is shown here, and "Retry now" tries them immediately. If every '
        'attempt fails, the enquiry is e-mailed to QCI’s inbox to be entered by hand.',
        'To change the matrix, edit its lists and the names of its levels here; the form uses '
        'the new lists at once. When Zoho\'s field names change, change the Ticket fields — no programming is '
        'needed.'])
shot('cms-helpdesk-matrix.png', 'The grievance matrix: the lists shown to a visitor who chooses QCI')

screen('Integrations', 'Site',
       'Certificate verification, the list of certified units, and the chatbot — each can be '
       'provided by another system and plugged in here.',
       'The Verify Certificate and Certified Units pages, and the "Ask about the scheme" '
       'assistant on every page.',
       'How it is provided: Not yet, Built in (chatbot only), Link, Page in a frame, Embed code or '
       'API. Then the '
       'title and introduction, and the details for the way chosen.',
       ['Not yet: the page says the service is being set up and points to the contact form. '
        'A page that is not yet on is also left out of the sitemap.',
        'Link: a button that opens the provider\'s own page in a new tab.',
        'Page in a frame: the provider\'s own page shown inside the portal page, at the '
        'height you set. Works only if the provider allows its page to be framed.',
        'Embed code: a snippet supplied by the provider, shown in a sealed-off frame on the page.',
        'API: the portal calls the provider\'s address and shows the answer in the portal\'s own '
        'design. Say where in the reply the results are, and which fields to show under which '
        'headings.',
        'Built in (chatbot): answers from the portal\'s own pages, FAQs, documents, notices and '
        'listings, including the text inside uploaded PDF and Word documents.',
        'Try it: type a sample value and press Run test. It uses the saved settings, so save '
        'first, and shows exactly what came back.'])

screen('Settings', 'Site',
       'Site-wide wording, contact details and switches. The mail server is under Enquiry mail, '
       'and the logos and colours under Branding.',
       'Throughout the site — headings, labels, footer text, contact details, and which features '
       'are shown at all.',
       'See the table below',
       ['Clearing a wording field removes what it labels rather than leaving a gap.',
        'Only what you actually changed is saved, so two people editing different tabs will not '
        'overwrite each other.'])

table(['Tab', 'What it controls'], [
    ['General', 'Site name, ministry name, logos, favicon'],
    ['Contact', 'Everything on the Contact page — headings, field labels, address, map, and '
                '"Ask which implementing agency the enquiry is for". Turn that off when there is '
                'only one agency, and name the agency enquiries should go to instead'],
    ['Features', 'Switches for the ticker, gallery band, success stories, assistant, '
                 'accessibility toolbar, translation, visitor counter, last-updated date and '
                 'maintenance mode'],
    ['Footer', 'Column headings, ownership line, last-updated date'],
    ['SEO', 'Default page title, description and keywords for pages that set none of their own'],
    ['Sidebar', 'The panels in the side rail of internal pages, and their switches'],
    ['Social', 'Social media addresses shown in the footer'],
    ['External links', 'Addresses of other government portals'],
    ['Application', 'The transactional LEAN system’s base address and registration URL'],
], widths=[1.5, 5.0])

screen('Users', 'Site',
       'Back-office accounts and the role each person holds.',
       'Nothing on the public site.',
       'Name, E-mail, Designation, Department, Role, Active, Must change password',
       ['A new account is created with a temporary password the person must change at first '
        'sign-in.'])

screen('Activity log', 'Site',
       'An immutable record of every administrative change, kept for audit.',
       'Nothing on the public site.',
       'Read-only: who, what, when and from which address')

doc.add_page_break()

# --------------------------------------------------------------- section 4 ----
doc.add_heading('4. Common tasks', level=1)

tasks = [
    ('Change a heading on the home page', 'Pages → Home → the band → edit its heading.'),
    ('Take a home page band off the site', 'Pages → Home → the band → turn off Visible.'),
    ('Add a slide to the hero', 'Banners → Add banner. Fill in the alternative text; it is required.'),
    ('Publish a circular', 'News & notices → Add, set Type to Circular, set Status to Published.'),
    ('Add a downloadable form', 'Media library → upload the file → copy its URL. '
                                'Documents → Add → paste the URL.'),
    ('Add a state incentive', 'Incentives → Add incentive → Category States / UTs → fill in the '
                              'State, the body offering it and who to contact.'),
    ('Change where QCI enquiries go', 'Enquiry mail → QCI → Enquiries inbox (used when Zoho '
                                      'Desk is off or cannot be reached).'),
    ('Replace the header or footer logo', 'Branding → the logo slot → Upload, set the address '
                                          'it opens, Save.'),
    ('Change the colours of the site', 'Branding → Colour theme → choose a theme → Save.'),
    ('Stop asking visitors to choose an agency', 'Settings → Contact → turn off "Ask which '
                                                 'implementing agency", and name the agency.'),
    ('Give NPC its own mail server', 'Enquiry mail → NPC → turn on the mail server switch (it '
                                     'reads "Sent from its own mail server") → fill in → Save → '
                                     'Test NPC’s route.'),
    ('Update the QCI grievance categories', 'Helpdesk (Zoho) → Grievance matrix.'),
    ('Switch on certificate verification', 'Integrations → Certificate verification → choose '
                                           'Link, Embed code or API → fill in → Try it → Save.'),
    ('Move a link to the bottom bar', 'Navigation → Footer bottom bar → Add item; then remove it '
                                      'from Footer policies.'),
    ('Add a link to the footer', 'Navigation → the Footer menu → Add item.'),
    ('Correct a phone number on Contact', 'Settings → Contact.'),
    ('Hide the chatbot', 'Settings → Features → turn off the assistant.'),
    ('Close the site for maintenance', 'Settings → Features → Maintenance mode. Visitors see a '
                                       '"temporarily unavailable" page; the admin console stays '
                                       'open, and signed-in staff still see the site. Turn it '
                                       'off the same way.'),
]
table(['To do this', 'Go here'], tasks, widths=[2.6, 3.9])

doc.add_page_break()

# --------------------------------------------------------------- section 5 ----
doc.add_heading('5. Enquiries and mail', level=1)

para('This works differently from the rest of the console and is worth reading once.')

doc.add_heading('What happens when somebody sends an enquiry', level=2)
bullets([
    'The visitor chooses which implementing agency the enquiry is for — unless the question is '
    'switched off under Settings → Contact, when it goes to the agency named there.',
    'The visitor answers the verification challenge (a picture of characters, or a written '
    'question for anyone who cannot see it).',
    'The enquiry is saved in the database.',
    'QCI: it is raised as a ticket in Zoho Desk, with the categories the visitor chose. If Zoho '
    'Desk is switched off, it is e-mailed to QCI instead. If Zoho cannot be reached, the portal '
    'keeps trying, and e-mails QCI only if every attempt fails.',
    'NPC (and any other agency): it is e-mailed to that agency’s Enquiries inbox — through the '
    'portal’s mail server, or the agency’s own if it has one.',
    'Enquiries are not listed in the console. E-mail and Zoho Desk are how an agency learns it '
    'has one.',
])
shot('site-contact-qci.png', 'The Contact Us form with QCI chosen: the category lists come from '
                             'the grievance matrix')

doc.add_heading('Setting it up', level=2)
para('All of it is under Enquiry mail and Helpdesk (Zoho). If neither mail nor Zoho is set up, '
     'enquiries are saved but nobody is told about them.')
table(['What', 'Where'], [
    ['The portal’s mail server', 'Enquiry mail → The portal’s mail server — server, port, '
                                 'encryption, sign-in name, password, "send as" address'],
    ['Each agency’s inbox', 'Enquiry mail → the agency → Enquiries inbox'],
    ['An agency’s own mail server', 'Enquiry mail → the agency → turn on the switch, so it reads '
                                    '"Sent from its own mail server"'],
    ['QCI’s tickets in Zoho Desk', 'Helpdesk (Zoho) → Connection, then Test the saved connection'],
], widths=[1.8, 4.7])

para('The password is stored encrypted and is never shown again once saved. Leave the field empty '
     'when saving to keep the password you already have; type a new one to replace it.')

doc.add_heading('Checking it works', level=2)
para('Open Enquiry mail. Every block — the portal’s mail server, and each agency — ends in a '
     '"Send a test to…" box: type an address you can read, and press Test the portal’s server, or '
     'Test QCI’s route, and so on. An agency’s test goes exactly the way its enquiries would, '
     'through its own server if it has one. Save before testing — the test uses what is saved.')
para('Under each agency, the hint beneath Enquiries inbox says where its enquiries go when the box '
     'is empty: the address the agency publishes, or the general enquiry address. If that is not '
     'a mailbox somebody reads, fill in the inbox.', italic=True)

doc.add_heading('For Microsoft 365 / Outlook', level=2)
bullets([
    'Server smtp.office365.com, port 587, encryption StartTls.',
    'Basic authentication for SMTP is off by default. An account with multi-factor '
    'authentication needs an app password, or SMTP AUTH has to be enabled for that mailbox.',
    'The mailbox signing in must be permitted to send as the "send as" address, or Microsoft 365 '
    'will reject the mail.',
])

doc.add_page_break()

# --------------------------------------------------------------- section 6 ----
doc.add_heading('6. When a change does not appear', level=1)

table(['What you see', 'Usually because'], [
    ['The page is not on the site', 'Its Status is not Published.'],
    ['An item is missing from a list', 'It is disabled, or its date range has passed.'],
    ['A whole band is missing', 'It is hidden in Pages → Home, or its switch is off under '
                               'Settings → Features, or it has no items to show.'],
    ['A heading has vanished', 'Its wording was cleared in Settings. Clearing removes rather '
                               'than blanks.'],
    ['An image does not load', 'The URL was pasted incompletely, or the file was removed from '
                               'the Media library.'],
    ['Filters are missing on an incentives page', 'None of its entries carry a level or a state.'],
    ['Enquiries are not arriving', 'Check Enquiry mail. Either no mail server is configured or '
                                   'the agency has no inbox set. For QCI, also Helpdesk (Zoho) → '
                                   'Test the saved connection, and the waiting count.'],
    ['A new logo or theme does not show', 'Reload the page. A page already open keeps the old '
                                          'look until it is reloaded.'],
    ['You were returned to the sign-in page', 'The console signs out after 30 minutes without '
                                              'activity. Sign in again; save before leaving the '
                                              'desk.'],
    ['Visitors see "Too many requests"', 'One address sent a great many requests in a minute. '
                                         'It clears by itself within a minute; tell the server '
                                         'team if it keeps happening.'],
    ['A button you need is missing', 'Your role does not permit that action.'],
], widths=[2.6, 3.9])

para('If none of these explain it, the Activity log will show whether the change was saved and by '
     'whom.')

doc.add_paragraph()
p = doc.add_paragraph()
p.alignment = WD_ALIGN_PARAGRAPH.CENTER
run = p.add_run('MSME Competitive (LEAN) Scheme portal — CMS user manual')
run.font.size = Pt(9)
run.font.color.rgb = GREY

out = r'D:\Lean Scheme\Lean Website\docs\LEAN-Portal-CMS-User-Manual.docx'
doc.save(out)
print('written:', out)
