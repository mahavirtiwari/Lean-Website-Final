"""
Builds the walkthrough video from the captured screenshots.

Each section gets two frames - the console screen where it is edited, then the
part of the website it changes - on a plain caption bar, so somebody watching can
follow the link between the two without narration.
"""
import os
import subprocess
import sys

import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont

SHOTS = sys.argv[1]
OUT = sys.argv[2]
FRAMES = os.path.join(os.path.dirname(OUT), '_frames')
os.makedirs(FRAMES, exist_ok=True)

W, H = 1920, 1080
TEAL = (15, 121, 137)
INK = (26, 32, 44)
GREY = (110, 120, 132)
BG = (244, 247, 251)


def font(size, bold=False):
    for name in (('seguisb.ttf' if bold else 'segoeui.ttf'), 'arialbd.ttf' if bold else 'arial.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


TITLE = font(46, bold=True)
SUB = font(28)
TAG = font(24, bold=True)


def frame(path, section, caption, kind, index):
    """One slide: a caption bar, then the screenshot scaled to fit beneath it."""
    canvas = Image.new('RGB', (W, H), BG)
    draw = ImageDraw.Draw(canvas)

    draw.rectangle([0, 0, W, 150], fill='white')
    draw.rectangle([0, 148, W, 152], fill=TEAL)

    draw.text((70, 34), section, font=TITLE, fill=INK)
    draw.text((70, 95), caption, font=SUB, fill=GREY)

    # A tag on the right saying which side of the pair this is.
    colour = TEAL if kind == 'In the CMS' else (176, 96, 24)
    tw = draw.textlength(kind, font=TAG)
    draw.rounded_rectangle([W - tw - 130, 52, W - 70, 102], radius=25, fill=colour)
    draw.text((W - tw - 100, 64), kind, font=TAG, fill='white')

    shot = Image.open(path).convert('RGB')
    box_w, box_h = W - 140, H - 150 - 90
    scale = min(box_w / shot.width, box_h / shot.height, 1.0)
    shot = shot.resize((int(shot.width * scale), int(shot.height * scale)), Image.LANCZOS)

    x = (W - shot.width) // 2
    y = 150 + (box_h - shot.height) // 2 + 20
    # A hairline so a white screenshot does not bleed into the background.
    draw.rectangle([x - 1, y - 1, x + shot.width, y + shot.height], outline=(214, 222, 232))
    canvas.paste(shot, (x, y))

    out = os.path.join(FRAMES, f'{index:03d}.png')
    canvas.save(out)
    return out


# section, caption, cms file, site file
PAIRS = [
    ('Banners', 'Slides in the hero carousel at the top of the home page',
     'cms-banners.png', None),
    ('Statistics', 'The counter strip — the scheme in numbers',
     'cms-statistics.png', 'site-statistics.png'),
    ('Login portals', 'The registration and sign-in tiles',
     'cms-login-portals.png', None),
    ('Benefits', 'The Benefits / Incentives cards on the home page',
     'cms-benefits.png', 'site-benefits.png'),
    ('Incentives', 'The entries listed on each Benefits / Incentives page',
     'cms-incentives.png', 'site-incentives.png'),
    ('Success stories', 'The success stories carousel',
     'cms-success-stories.png', 'site-success-stories.png'),
    ('Components', 'The six components of the scheme',
     'cms-components.png', 'site-components.png'),
    ('Partners', 'The partners strip, and the agencies on Contact Us',
     'cms-partners.png', 'site-partners.png'),
    ('Gallery', 'Photo and film albums',
     'cms-gallery.png', 'site-gallery.png'),
    ('Documents', 'Guidelines, brochures and formats offered for download',
     'cms-documents.png', 'site-downloads.png'),
    ('News & notices', 'Announcements, circulars and tenders',
     'cms-posts.png', 'site-documents.png'),
    ('FAQs', 'The questions on the FAQs page',
     'cms-faqs.png', 'site-faqs.png'),
    ('Navigation', 'Every menu on the site, including the footer columns and bottom bar',
     'cms-navigation.png', 'site-footer.png'),
    ('Branding', 'Header and footer logos, their links, and the colour theme',
     'cms-branding.png', None),
    ('Pages', 'Every page on the site except the home page',
     'cms-pages.png', None),
    ('Scheme levels', 'The Pledge and the Bronze, Silver and Gold tiers',
     'cms-scheme-levels.png', None),
    ('Programmes', 'Awareness programmes and assessor training',
     'cms-programmes.png', None),
    ('Settings', 'Site-wide wording, contact details and switches',
     'cms-settings.png', None),
    ('Enquiry mail', 'The mail servers, and where each agency’s enquiries go',
     'cms-enquiry-mail.png', None),
    ('Helpdesk (Zoho)', 'QCI’s enquiries as Zoho Desk tickets',
     'cms-helpdesk.png', 'site-contact-qci.png'),
    ('Integrations', 'Certificate verification, certified units and the chatbot',
     'cms-integrations.png', None),
    ('Media library', 'Every image and file uploaded to the portal',
     'cms-media.png', None),
    ('Users', 'Back-office accounts and roles',
     'cms-users.png', None),
    ('Activity log', 'A record of every administrative change',
     'cms-activity.png', None),
]

index, made = 0, []
for section, caption, cms, site in PAIRS:
    for path, kind in ((cms, 'In the CMS'), (site, 'On the website')):
        if not path:
            continue
        full = os.path.join(SHOTS, path)
        if not os.path.exists(full):
            print(f'  missing, skipped: {path}')
            continue
        made.append(frame(full, section, caption, kind, index))
        index += 1

print(f'{len(made)} frames')

# Three seconds a frame, encoded for playback anywhere.
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
subprocess.run([
    ffmpeg, '-y', '-loglevel', 'error',
    '-framerate', '1/3', '-i', os.path.join(FRAMES, '%03d.png'),
    '-c:v', 'libx264', '-r', '25', '-pix_fmt', 'yuv420p',
    '-vf', 'scale=1920:1080:force_original_aspect_ratio=decrease,pad=1920:1080:(ow-iw)/2:(oh-ih)/2',
    OUT,
], check=True)

print('written:', OUT, f'({os.path.getsize(OUT) // 1024} KB)')
