# =============================================================================
#  LEAN portal - production settings for Go-Live.ps1
#
#  Review every value below before the first run. Nothing secret goes in this
#  file: the SQL administrator password, the first CMS administrator password
#  and the PFX password are all asked for while the script runs, and never
#  written here.
# =============================================================================
@{
    # ------------------------------------------------------------------ site --
    # The public address. The certificate must cover it (*.qci.org.in does).
    HostName          = 'leannew.qci.org.in'

    # IIS site and application pool names. The API pool is "<SiteName>-Api".
    SiteName          = 'LeanPortal'

    # Where the site is installed. Keep it off the folder the code was
    # downloaded to: a later download replaces that folder, not this one.
    SiteRoot          = 'C:\inetpub\LeanPortal'

    # Stop IIS's "Default Web Site" if it is still the untouched default, so the
    # server's bare IP address does not show the IIS welcome page. Any site that
    # has been changed from the default is left alone.
    StopDefaultWebSite = $true

    # Open TCP 80 and 443 in Windows Firewall. A network firewall, WAF or cloud
    # security group in front of the server still has to be opened by its owner.
    OpenFirewall      = $true

    # ------------------------------------------------------------- database --
    # SQL Server instance: 'localhost', 'localhost\SQLEXPRESS', or another
    # server's name. 'localhost' means SQL Server runs on this web server.
    SqlServer         = 'localhost'
    DatabaseName      = 'LeanPortal'

    # How Go-Live.ps1 itself signs in to SQL Server to create the database:
    #   'Windows' - as the Windows account running the script (it must be a
    #               sysadmin on the instance)
    #   'Sql'     - as a SQL login such as sa; the script asks for it
    SqlAdminAuth      = 'Windows'

    # How the portal signs in to SQL Server once it is live:
    #   'AppPoolIdentity' - the IIS application pool's own identity, so no
    #                       password is stored anywhere. SQL Server must be on
    #                       this machine.
    #   'SqlLogin'        - a dedicated SQL login (below) with a long random
    #                       password the script generates. Use this when SQL
    #                       Server is on another machine.
    SqlAppAuth        = 'AppPoolIdentity'
    SqlAppLogin       = 'leanportal_app'

    # Leave empty to use the instance's default data and log folders.
    SqlDataPath       = ''
    SqlLogPath        = ''

    # FULL keeps point-in-time recovery and needs the log backups the Backups
    # step schedules every 15 minutes. SIMPLE needs only the nightly backup.
    RecoveryModel     = 'FULL'

    # Accept the SQL Server's own certificate. $true is right when SQL Server
    # is on this machine; for a remote server use a certificate the web server
    # trusts and set this to $false.
    SqlTrustServerCertificate = $true

    # ----------------------------------------------------------------- admin --
    # The first CMS administrator. Its password is asked for once, used on the
    # very first start, and removed from the settings file after the account
    # exists. It must be changed at first sign-in.
    AdminEmail        = 'lean-admin@qci.org.in'

    # --------------------------------------------------------------- HTTPS --
    # The QCI wildcard certificate and GoDaddy's intermediate bundle. Leave
    # empty to have the script look for a single .pfx / .p7b in the code
    # folder, E:\certs and C:\certs.
    PfxPath           = ''
    ChainPath         = ''

    # Switch off TLS 1.0 / 1.1 and SSL 3.0 for the whole server (GIGW and
    # CERT-In audits expect TLS 1.2+). This affects every program on the
    # machine and needs a restart, so it is off unless you choose it.
    DisableLegacyTls  = $false

    # ------------------------------------------------------------- backups --
    # Nightly database + uploads + keys backup, and log backups every 15 min.
    # When SQL Server is on another machine, SqlBackupPath must be a folder on
    # that machine (or a share it can write to).
    BackupRoot        = 'E:\LeanPortalBackups'
    SqlBackupPath     = ''
    BackupRetainDays  = 14
    BackupTime        = '02:00'

    # ------------------------------------------------------------- network --
    # Only if a load balancer or WAF sits in front of IIS: its IP addresses, so
    # visitors' real addresses are used for rate limits and logs.
    KnownProxies      = @()
}
