using LeanPortal.Domain.Common;
using LeanPortal.Domain.Entities;
using LeanPortal.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LeanPortal.Infrastructure.Persistence.Seed;

public static partial class DataSeeder
{
    /// <summary>
    /// Creates the public page tree. Body copy is derived from the content published on
    /// lean.msme.gov.in and is fully editable from the CMS after the first deployment.
    /// </summary>
    private static async Task SeedPagesAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Pages.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;

        Page P(string slug, string title, string? body, int order,
            PageTemplate template = PageTemplate.SidebarLeft, string? summary = null,
            string? shortTitle = null, string? customComponent = null, string? metaDescription = null,
            string? eyebrow = null) => new()
        {
            Slug = slug,
            Title = title,
            ShortTitle = shortTitle,
            Summary = summary,
            Body = body,
            Template = template,
            CustomComponent = customComponent,
            BannerCaption = eyebrow,
            SortOrder = order,
            Status = PublishStatus.Published,
            PublishedAt = now,
            MetaTitle = title,
            MetaDescription = metaDescription ?? summary,
            ShowSidebarNav = template == PageTemplate.SidebarLeft
        };

        // ---------------------------------------------------------------- home ----
        var home = P("home", "Home", null, 0, PageTemplate.Blocks,
            summary: "Official portal of the MSME Competitive (LEAN) Scheme, Ministry of MSME, Government of India.",
            metaDescription: "MSME Competitive (LEAN) Scheme - raise productivity, quality and competitiveness " +
                             "with up to 90% subsidy on consultant fees. Register with your Udyam number.");
        home.ShowSidebarNav = false;
        db.Pages.Add(home);

        // -------------------------------------------------------- about scheme ----
        var about = P("about-scheme", "About the Scheme",
            """
            <p>The <strong>MSME Competitive (LEAN) Scheme</strong> is an initiative of the Ministry of Micro, Small
            and Medium Enterprises, Government of India, to enhance the productivity, efficiency and competitiveness
            of Indian MSMEs by reducing waste in processes, inventory, space and energy consumption.</p>
            <p>Use the sections in this part of the portal to understand why the scheme exists, what it offers,
            who is eligible, how the three implementation levels work, and what financial assistance is available.</p>
            """, 1,
            summary: "Everything about the MSME Competitive (LEAN) Scheme: objective, components, levels, " +
                     "eligibility, certification and financial assistance.",
            shortTitle: "About Scheme");
        db.Pages.Add(about);
        await db.SaveChangesAsync(ct);

        var introduction = P("about-scheme/introduction", "Introduction",
            """
            <p>As domestic and global competitiveness becomes intensive, there is a need for MSMEs to transition to
            a new business environment, especially with the disruption in global supply chains and the convergence
            of multiple sourcing as a methodology in vendor development.</p>

            <p>MSMEs form an integral part of almost every value chain and there is a symbiotic relationship
            between large corporations and relatively small sized suppliers. Recognising the importance of the
            overall economic growth of a country and the need for enhancing its productivity, competitiveness and
            employment generation besides resource optimisation, many countries have initiated institutional
            mechanisms for a national approach on improving the quality of manufacturing and services.</p>

            <p>The Ministry of Micro, Small &amp; Medium Enterprises, Government of India, aims to implement the
            <strong>MSME Competitive (LEAN) Scheme</strong> for MSMEs with an objective to enhance their
            productivity, efficiency and competitiveness by reduction of wastages in processes, inventory
            management, space management, energy consumption and more.</p>

            <h3>What is Lean Manufacturing?</h3>
            <p>Lean manufacturing considers the expenditure of resources for any goal, other than the creation of
            value for the end customer, to be wasteful and therefore a target for elimination. Working from the
            perspective of the customer who consumes a product or service, value is defined as any action or
            process that a customer is willing to pay for. Lean is centred on preserving value with less work,
            through continuous improvement - <em>kaizen</em>.</p>
            """, 1, summary: "Why the Ministry of MSME launched the Competitive (LEAN) Scheme, and what " +
                             "Lean manufacturing means for an Indian MSME.");
        introduction.ParentId = about.Id;

        var objective = P("about-scheme/objective", "Objective",
            """
            <p>The scheme aims to strengthen the domestic and global competitiveness of MSMEs through Lean
            techniques. Its objectives are grouped under three themes.</p>

            <h3>Reduction in</h3>
            <ul>
              <li>Rejection rates</li>
              <li>Product and raw material movements</li>
              <li>Product cost</li>
            </ul>

            <h3>Optimisation of</h3>
            <ul>
              <li>Space utilisation</li>
              <li>Resources such as water, energy and natural resources</li>
            </ul>

            <h3>Enhancement of</h3>
            <ul>
              <li>Quality in process and product</li>
              <li>Production and export capabilities</li>
              <li>Workplace safety</li>
              <li>Knowledge and skill sets</li>
              <li>Innovative work culture</li>
              <li>Social and environmental accountability</li>
              <li>Profitability</li>
              <li>Introduction and awareness to Industry 4.0</li>
              <li>Digital empowerment</li>
            </ul>
            """, 2, summary: "The reduction, optimisation and enhancement goals that the LEAN Scheme sets " +
                             "for participating enterprises.");
        objective.ParentId = about.Id;

        var components = P("about-scheme/scheme-components", "Scheme Components",
            """
            <p>The MSME Competitive (LEAN) Scheme is delivered through six components that together cover
            awareness, capability building, implementation support, incentives, publicity and technology.</p>

            <h3>I. Industry Awareness Programmes / Workshops</h3>
            <p>MSMEs will be made aware of the scheme through nation-wide awareness programmes, online and/or
            face to face as appropriate, with the support of industry associations and government agencies.</p>

            <h3>II. Training Programmes</h3>
            <p>Stakeholders such as MSME officers, assessors and consultants will be trained on the MSME
            Competitive (LEAN) Scheme, enabling effective implementation by agencies such as QCI and NPC.</p>

            <h3>III. Handholding</h3>
            <p>MSMEs will be provided handholding towards the implementation of Lean tools and techniques at three
            different levels - Bronze, Silver and Gold - with a verifiable assessment at each stage.</p>

            <h3>IV. Benefits / Incentives</h3>
            <p>Graded incentives will be announced by the Ministry of MSME to encourage the participation of MSME
            units under the scheme, linked to achievements such as generating a LEAN ID, completing the LEAN Pledge
            and implementing Lean at various levels.</p>

            <h3>V. PR Campaign, Advertising and Brand Promotion</h3>
            <p>For popularising the LEAN Scheme, nation-wide publicity will be undertaken.</p>

            <h3>VI. Digital Platform</h3>
            <p>The LEAN Scheme process is e-enabled through a single-window digital platform which is utilised for
            the implementation of the scheme end to end.</p>
            """, 3, summary: "The six components of the scheme: awareness, training, handholding, incentives, " +
                             "publicity and the single-window digital platform.");
        components.ParentId = about.Id;

        var levels = P("about-scheme/scheme-levels", "Scheme Levels",
            """
            <h3>The LEAN Pledge</h3>
            <p>Every MSME that embarks on the journey of Lean (Bronze, Silver, Gold) will have to take a
            <strong>LEAN Pledge</strong>. The intent of taking a Lean Pledge is to take a pre-commitment, or a
            solemn promise by MSMEs to uphold the values of Lean practices and philosophy in their functioning.</p>

            <p>After taking the LEAN Pledge, the MSME can apply for handholding of its units depending on the need,
            level of preparedness and interest of the MSME unit. The LEAN Scheme can be implemented by MSME units
            at three levels.</p>
            """, 4, summary: "The LEAN Pledge and the three implementation levels - Bronze, Silver and " +
                             "Gold - with deliverables and fees for each.");
        levels.ParentId = about.Id;

        var eligibility = P("about-scheme/coverage-eligibility", "Coverage & Eligibility",
            """
            <p>The scheme is applicable to all states and union territories of India, and at present is designed for
            manufacturing MSMEs. A second phase of the scheme is proposed to be opened for the service sector.</p>

            <ol>
              <li>All MSMEs registered with the <strong>UDYAM registration portal</strong> of the Ministry of MSME
              will be eligible to participate in the MSME Competitive (LEAN) Scheme and avail related
              benefits and incentives.</li>
              <li>Common Facilities Centres (CFCs) operating under the <strong>SFURTI</strong> (Scheme of Fund for
              Regeneration of Traditional Industries) and <strong>MSE-CDP</strong> (Micro &amp; Small Enterprises
              Cluster Development Programme) schemes are also eligible to participate.</li>
            </ol>

            <div class="callout callout--info">
              <p><strong>Do not have a Udyam registration?</strong> Register free of cost on the
              <a href="https://udyamregistration.gov.in/" target="_blank" rel="noopener noreferrer">Udyam
              Registration portal</a> first. A Udyam Registration Number is mandatory to apply for the LEAN Scheme.</p>
            </div>
            """, 5, summary: "Who can participate in the LEAN Scheme, including MSMEs registered on Udyam and " +
                             "Common Facilities Centres under SFURTI and MSE-CDP.",
            shortTitle: "Coverage & Eligibility");
        eligibility.ParentId = about.Id;

        var certificate = P("about-scheme/e-certificate", "E-Certificate",
            """
            <p>Recognition under the MSME Competitive (LEAN) Scheme is issued digitally, so it can be verified
            instantly by customers, OEMs and financial institutions.</p>

            <ol type="a">
              <li>An E-Certificate towards participation under the scheme will be issued by the Ministry of MSME
              after completion of the <strong>Bronze Level</strong>, <strong>Silver Level</strong> and
              <strong>Gold Level</strong>.</li>
              <li>The list of MSMEs that have taken the LEAN Pledge and achieved any Lean implementation level
              will be displayed on the LEAN Scheme portal.</li>
            </ol>

            <p>Certificates are issued for the LEAN Pledge and for each of the three implementation levels, and can
            be downloaded from the MSME dashboard once the corresponding assessment is cleared.</p>
            """, 6, summary: "How LEAN e-Certificates are issued for the Pledge and for the Bronze, " +
                             "Silver and Gold levels.");
        certificate.ParentId = about.Id;

        var assistance = P("about-scheme/financial-assistance", "Financial Assistance",
            """
            <p>The Ministry of MSME meets the greater part of the cost of Lean implementation so that the
            investment required from a small enterprise stays affordable.</p>

            <h3>For MSME units</h3>
            <ul>
              <li><strong>90% subsidy</strong> on the implementation cost of consultant fees for the MSME.</li>
              <li>An <strong>additional 5%</strong> contribution from the Government of India for MSMEs that
              register through an Industry Association or an OEM, granted after completion of all intervention
              levels.</li>
              <li>An <strong>additional 5% subsidy</strong> for MSMEs owned by Women, SC and ST entrepreneurs.</li>
            </ul>

            <h3>For OEMs and Industry Associations</h3>
            <ul>
              <li><strong>&#8377; 5,000 per MSME</strong> is payable to the OEM or Association after the MSME
              completes all stages of Lean intervention.</li>
            </ul>

            <h3>Fee structure by level</h3>
            <table class="data-table">
              <thead>
                <tr><th scope="col">Level</th><th scope="col">Cost to the MSME</th></tr>
              </thead>
              <tbody>
                <tr><td>Bronze</td><td>Free of cost</td></tr>
                <tr><td>Silver</td><td>90% subsidy on the implementation cost of the consultant fees</td></tr>
                <tr><td>Gold</td><td>90% subsidy on the implementation cost of the consultant fees</td></tr>
              </tbody>
            </table>
            """, 7, summary: "Subsidy of 90% on consultant fees, additional 5% for association or OEM routed " +
                             "registrations and for Women, SC and ST owned enterprises.");
        assistance.ParentId = about.Id;

        db.Pages.AddRange(introduction, objective, components, levels, eligibility, certificate, assistance);

        // ------------------------------------------------- implementing agency ----
        var agency = P("implementation-agency", "Implementation Agency",
            """
            <p>The MSME Competitive (LEAN) Scheme is implemented nation-wide through implementing agencies on
            behalf of the Ministry of MSME. The agencies empanel consultant organisations and assessors, run
            awareness and training programmes, allocate handholding assignments and conduct assessments.</p>

            <h3>Quality Council of India (QCI)</h3>
            <p>QCI is the national body for quality, established by the Government of India jointly with Indian
            industry. Under the LEAN Scheme it empanels consultant organisations and assessors and delivers
            awareness and training programmes across the country.</p>

            <h3>National Productivity Council (NPC)</h3>
            <p>NPC is an autonomous organisation under the Department for Promotion of Industry and Internal Trade.
            It provides productivity consultancy, trains assessors and consultants, and supports MSMEs through the
            handholding phases of the scheme.</p>

            <h3>How consultants are allocated</h3>
            <p>The allocation of consultants is done by the implementing agency through a pre-defined process.
            MSMEs may provide a list of preferred consultant organisations to the implementing agency. Selection
            uses a financial and technical bidding process in which technical proficiency carries 70% weightage and
            the financial bid carries 30% weightage.</p>
            """, 2, summary: "QCI and NPC implement the LEAN Scheme nation-wide on behalf of the Ministry of MSME.",
            shortTitle: "Implementation Agency");
        db.Pages.Add(agency);

        // ------------------------------------------------------------ register ----
        var register = P("register", "Register for the LEAN Scheme",
            """
            <p>Registration for the MSME Competitive (LEAN) Scheme is entirely online and free. All you need is
            your Udyam Registration Number and the mobile number linked to it.</p>
            """, 3, summary: "Apply for the LEAN Scheme, register as an OEM or Industry Association, and " +
                             "understand the benefits available to your enterprise.",
            shortTitle: "Register");
        db.Pages.Add(register);
        await db.SaveChangesAsync(ct);

        var howTo = P("register/how-to-register", "How to Register",
            """
            <p>The registration process takes eight steps and is completed in a single sitting.</p>

            <ol class="step-list">
              <li><strong>Apply for the LEAN Scheme</strong> - select this option from the registration menu and
              enter the required details.</li>
              <li><strong>Enter your Udyam Registration Number</strong> - provide your Udyam Registration Number and
              the mobile number registered against it for verification through the Udyam portal.</li>
              <li><strong>Not registered with Udyam?</strong> - complete your registration on the Udyam portal at
              udyamregistration.gov.in first, then return to this portal.</li>
              <li><strong>Validate your Udyam details</strong> - enter the OTP sent to your registered mobile
              number.</li>
              <li><strong>Verify your Udyam details</strong> - your enterprise information is retrieved
              automatically from the Udyam portal for confirmation.</li>
              <li><strong>LEAN Coordinator details</strong> - nominate a representative from your MSME who will
              coordinate the LEAN journey, and verify their contact details.</li>
              <li><strong>Implementing agency</strong> - select your implementing agency and indicate whether you
              are part of a SFURTI cluster, an Industry Association, or a large OEM.</li>
              <li><strong>Registration complete</strong> - your LEAN ID and password are sent to your registered
              e-mail address and you are redirected to the login page.</li>
            </ol>

            <div class="callout callout--info">
              <p><strong>After registration.</strong> On logging in you must review and accept both the Undertaking
              and the LEAN Pledge before the e-modules become available to you.</p>
            </div>
            """, 1, summary: "The eight steps from Udyam verification to receiving your LEAN ID.");
        howTo.ParentId = register.Id;

        var benefits = P("register/benefits-to-msme", "Benefits to MSMEs",
            """
            <p>Participating in the LEAN Scheme gives a manufacturing MSME structured, subsidised access to the
            tools that large enterprises have used for decades to remove cost and improve quality.</p>

            <h3>Operational benefits</h3>
            <ul>
              <li>Lower rejection rates and rework, and better first-time-right quality</li>
              <li>Reduced inventory, shorter lead times and improved on-time delivery</li>
              <li>Better utilisation of factory space, energy, water and raw material</li>
              <li>Safer, better organised and more productive workplaces</li>
              <li>Improved capacity without fresh capital investment</li>
            </ul>

            <h3>Commercial benefits</h3>
            <ul>
              <li>Lower product cost and improved margins</li>
              <li>Stronger position as a supplier in OEM and global value chains</li>
              <li>Recognised e-Certification from the Ministry of MSME at each level</li>
              <li>Listing on the LEAN portal among certified enterprises</li>
            </ul>

            <h3>Financial benefits</h3>
            <ul>
              <li>Bronze level is free of cost</li>
              <li>90% subsidy on consultant fees at Silver and Gold levels</li>
              <li>Additional 5% for units registering through an Association or OEM</li>
              <li>Additional 5% for units owned by Women, SC and ST entrepreneurs</li>
            </ul>

            <h3>Capability benefits</h3>
            <ul>
              <li>Trained internal teams that can sustain improvement without external help</li>
              <li>An innovative, problem-solving work culture built on kaizen</li>
              <li>Introduction and awareness to Industry 4.0 and digital manufacturing</li>
            </ul>
            """, 2, summary: "Operational, commercial, financial and capability benefits of LEAN certification " +
                             "for a manufacturing MSME.");
        benefits.ParentId = register.Id;

        db.Pages.AddRange(howTo, benefits);

        // --------------------------------------------------------- custom pages ----
        db.Pages.AddRange(
            P("register-as-consultant", "Register as a Consultant",
                """
                <p>Consulting organisations empanelled by the Quality Council of India and the National Productivity
                Council carry out the handholding activity under the LEAN Scheme. Consultants from these empanelled
                organisations work directly with MSME units through the Silver and Gold levels.</p>

                <h3>Who can apply</h3>
                <ul>
                  <li>Consulting organisations with demonstrable Lean and productivity improvement experience</li>
                  <li>Individual professionals with a background in manufacturing, industrial engineering or quality</li>
                  <li>Professionals willing to be trained and certified as LEAN assessors</li>
                </ul>

                <h3>How selection works</h3>
                <p>Allocation of assignments is done by the implementing agency through a pre-defined process using
                a financial and technical bidding process, in which technical proficiency carries 70% weightage and
                the financial bid carries 30% weightage.</p>
                """, 4,
                summary: "Empanelment for consultant organisations and assessors under the LEAN Scheme."),

            P("programmes/awareness", "Awareness Programmes", null, 1, PageTemplate.Custom,
                summary: "Nation-wide awareness programmes on the LEAN Scheme for MSMEs, listed by state and district.",
                customComponent: "programmes", eyebrow: "Capacity building"),

            P("programmes/training", "Training Programmes", null, 2, PageTemplate.Custom,
                summary: "Training programmes for assessors and consultants delivered by QCI and NPC.",
                customComponent: "programmes", eyebrow: "Capacity building"),

            P("gallery", "Gallery", null, 5, PageTemplate.Custom,
                summary: "Photographs from LEAN Scheme awareness programmes, workshops and factory visits.",
                customComponent: "gallery", eyebrow: "Media centre"),

            P("media/news", "News & Announcements", null, 6, PageTemplate.Custom,
                summary: "Latest news, announcements, circulars and tenders under the MSME Competitive (LEAN) Scheme.",
                shortTitle: "News", customComponent: "news", eyebrow: "Media centre"),

            P("downloads", "Downloads", null, 7, PageTemplate.Custom,
                summary: "Scheme guidelines, brochures, circulars, formats and presentations available for download.",
                customComponent: "downloads", eyebrow: "Resources"),

            P("faqs", "Frequently Asked Questions", null, 8, PageTemplate.Custom,
                summary: "Answers to the questions MSMEs ask most often about the LEAN Scheme.",
                shortTitle: "FAQs", customComponent: "faqs", eyebrow: "Help"),

            P("contact-us", "Contact Us", null, 9, PageTemplate.Custom,
                summary: "Get in touch with the LEAN Scheme team at the Ministry of MSME and its implementing agencies.",
                shortTitle: "Contact", customComponent: "contact", eyebrow: "Get in touch"),

            // ---------------------------------------------------------- policies ----
            P("policies/copyright-policy", "Copyright Policy",
                """
                <p>Material featured on this portal may be reproduced free of charge in any format or medium
                provided it is reproduced accurately and not used in a derogatory manner or in a misleading
                context. Where the material is being published or issued to others, the source must be prominently
                acknowledged.</p>
                <p>The permission to reproduce this material does not extend to any material on this site which is
                identified as being the copyright of a third party. Authorisation to reproduce such material must be
                obtained from the copyright holders concerned.</p>
                """, 1, summary: "Terms under which content from this portal may be reproduced."),

            P("policies/hyperlinking-policy", "Hyperlinking Policy",
                """
                <h3>Links to external websites</h3>
                <p>At many places on this portal you will find links to other websites and portals. These links have
                been placed for your convenience. The Ministry of MSME is not responsible for the contents and
                reliability of the linked websites and does not necessarily endorse the views expressed in them.
                The mere presence of a link, or its listing on this portal, should not be assumed as an endorsement
                of any kind.</p>

                <h3>Links to this portal</h3>
                <p>We do not object to you linking directly to information hosted on this portal and no prior
                permission is required for the same. However, we would like you to inform us about any links
                provided to this portal so that you can be informed of any change or updates. Also, we do not
                permit our pages to be loaded into frames on your site. The pages belonging to this portal must
                load into a newly opened browser window of the user.</p>
                """, 2, summary: "How this portal links to other websites, and how you may link to it."),

            P("policies/privacy-policy", "Privacy Policy",
                """
                <p>This portal does not automatically capture any specific personal information from you (such as
                name, telephone number or e-mail address) that allows us to identify you individually.</p>

                <h3>Information you choose to provide</h3>
                <p>If this portal requests or you choose to provide personal information, it is used only for the
                purpose for which it was collected, such as responding to your enquiry, registering your enterprise
                for the scheme, or sending you the newsletter you subscribed to. We do not sell or share this
                information with any third party, except where required to do so by law or where the information is
                required by an implementing agency to deliver the scheme to you.</p>

                <h3>Site visit data</h3>
                <p>This portal records your visit and logs the following information for statistical purposes: your
                server address, the name of the top-level domain from which you access the internet, the type of
                browser you use, the date and time you accessed the site, the pages you accessed and the documents
                you downloaded. No attempt is made to identify users or their browsing activities except in the
                event of an investigation, where a law enforcement agency may exercise a warrant to inspect logs.</p>

                <h3>Cookies</h3>
                <p>This portal uses only the cookies necessary for the site to function and to remember your
                accessibility preferences. No advertising or third-party tracking cookies are set.</p>
                """, 3, summary: "How personal information and site visit data are handled on this portal."),

            P("policies/terms-and-conditions", "Terms & Conditions",
                """
                <p>This portal is designed, developed and maintained for the Ministry of Micro, Small and Medium
                Enterprises, Government of India.</p>
                <p>Though all efforts have been made to ensure the accuracy and currency of the content on this
                portal, the same should not be construed as a statement of law or used for any legal purposes. In
                case of any ambiguity or doubt, users are advised to verify with the scheme guidelines, and to
                contact the Ministry of MSME or its implementing agencies.</p>
                <p>Under no circumstances will the Ministry be liable for any expense, loss or damage including,
                without limitation, indirect or consequential loss or damage, or any expense, loss or damage
                whatsoever arising from use, or loss of use, of data, arising out of or in connection with the use
                of this portal.</p>
                <p>These terms and conditions shall be governed by and construed in accordance with the Indian
                laws. Any dispute arising under these terms and conditions shall be subject to the jurisdiction of
                the courts of India.</p>
                """, 4, summary: "Terms governing the use of the LEAN Scheme portal.",
                shortTitle: "Terms & Conditions"),

            P("policies/accessibility-statement", "Accessibility Statement",
                """
                <p>We are committed to ensuring that this portal is accessible to all users irrespective of device,
                technology or ability. It has been built to conform with the <strong>Guidelines for Indian
                Government Websites (GIGW)</strong> and <strong>WCAG 2.1 Level AA</strong>.</p>

                <h3>Accessibility features</h3>
                <ul>
                  <li>An accessibility toolbar in the header adjusts text size, spacing, contrast, saturation and
                  typeface, and offers a reading mask, link highlighting and read-aloud</li>
                  <li>A high-contrast theme and a colour-inversion mode are available for users with low vision</li>
                  <li>Every page can be operated with a keyboard alone, with a visible focus indicator</li>
                  <li>A skip-to-main-content link is provided at the start of every page</li>
                  <li>All informative images carry alternative text</li>
                  <li>Content structure uses correct headings and landmarks for screen readers</li>
                </ul>

                <h3>Reporting an accessibility problem</h3>
                <p>If you experience any difficulty in accessing content on this portal, please write to us through
                the Contact Us page with the page address and a description of the problem, and we will respond
                within seven working days.</p>
                """, 5, summary: "Our commitment to GIGW and WCAG 2.1 AA, and the accessibility features " +
                                 "available on this portal.", shortTitle: "Accessibility"),

            P("policies/disclaimer", "Disclaimer",
                """
                <p>The information contained in this portal is for general information purposes only and is
                published by the Ministry of Micro, Small and Medium Enterprises, Government of India.</p>
                <p>While every effort is made to keep the information up to date and correct, no representation or
                warranty of any kind, express or implied, is made about the completeness, accuracy, reliability or
                suitability of the information contained on the portal for any purpose. Any reliance you place on
                such information is therefore strictly at your own risk.</p>
                <p>In case of any variance between what has been stated here and the scheme guidelines issued by
                the Ministry, the guidelines shall prevail.</p>
                """, 6, summary: "Limits of the information published on this portal."),

            P("sitemap", "Sitemap", null, 10, PageTemplate.Custom,
                summary: "A complete index of every section and page on the LEAN Scheme portal.",
                customComponent: "sitemap"),

            // Editable content rather than a coded screen: the list of assistive tools
            // changes over time and the accessibility team must be able to revise it.
            P("screen-reader-access", "Screen Reader Access",
                """
                <p>This portal conforms to the <strong>Guidelines for Indian Government Websites (GIGW)</strong>
                and <strong>WCAG 2.1 Level AA</strong>. Content is structured with correct headings and landmarks,
                all informative images carry alternative text, and every function can be reached with a keyboard
                alone.</p>

                <h3>Accessibility features on this portal</h3>
                <ul>
                  <li>The accessibility toolbar in the header enlarges or reduces text, widens letter and line
                  spacing, and switches to a dyslexia-friendly typeface</li>
                  <li>A high-contrast theme, a colour-inversion mode and reduced-saturation modes are available
                  for readers with low vision or colour sensitivity</li>
                  <li>A reading mask, link highlighting and a large cursor help with tracking and focus</li>
                  <li>Any page can be read aloud with the Read Aloud control</li>
                  <li>A skip-to-main-content link is the first focusable item on every page, and a visible focus
                  indicator marks the element you are on</li>
                  <li>Auto-scrolling content can be paused, and motion is reduced automatically when your device
                  asks for it</li>
                  <li>The Bhashini language selector in the header translates the portal into Indian languages</li>
                </ul>

                <h3>Screen readers you can use</h3>
                <p>The portal has been tested with the following screen readers. Each link opens the provider's
                own site in a new tab.</p>

                <table>
                  <caption>Screen reader software compatible with this portal</caption>
                  <thead>
                    <tr>
                      <th scope="col">Screen reader</th>
                      <th scope="col">Provider</th>
                      <th scope="col">Availability</th>
                      <th scope="col">Download</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <th scope="row">NVDA</th><td>NV Access</td><td>Free</td>
                      <td><a href="https://www.nvaccess.org/download/" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                    <tr>
                      <th scope="row">JAWS</th><td>Freedom Scientific</td><td>Commercial</td>
                      <td><a href="https://www.freedomscientific.com/products/software/jaws/" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                    <tr>
                      <th scope="row">Narrator</th><td>Microsoft</td><td>Built into Windows</td>
                      <td><a href="https://support.microsoft.com/windows/complete-guide-to-narrator-e4397a0d-ef4f-b386-d8ae-c172f109bdb1" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                    <tr>
                      <th scope="row">VoiceOver</th><td>Apple</td><td>Built into macOS and iOS</td>
                      <td><a href="https://www.apple.com/accessibility/vision/" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                    <tr>
                      <th scope="row">TalkBack</th><td>Google</td><td>Built into Android</td>
                      <td><a href="https://support.google.com/accessibility/android/answer/6283677" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                    <tr>
                      <th scope="row">Orca</th><td>GNOME</td><td>Free</td>
                      <td><a href="https://help.gnome.org/users/orca/stable/" target="_blank" rel="noopener noreferrer">Visit site</a></td>
                    </tr>
                  </tbody>
                </table>

                <h3>Telling us about a problem</h3>
                <p>If any part of this portal is difficult to use with your assistive technology, please tell us
                which page and what happened through the Contact Us page. We respond within seven working days.</p>
                """, 11, PageTemplate.FullWidth,
                summary: "Information about screen reader compatibility and the assistive tools supported.",
                shortTitle: "Screen Reader", eyebrow: "Accessibility")
        );

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} content pages.", await db.Pages.CountAsync(ct));
    }
}
