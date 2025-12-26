using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectsWebApp.Models;
using ProjectsWebApp.Utility;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;

namespace ProjectsWebApp.DataAccsess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

      
        public DbSet<AplicationUser> AplicationUser { get; set; }

        public DbSet<UserSecurityState> UserSecurityStates { get; set; }

        public DbSet<PortalCard> PortalCards { get; set; }
        public DbSet<PortalVideo> PortalVideo { get; set; }
        public DbSet<MitmachenContent> MitmachenContents { get; set; }
        public DbSet<SliderItem> SliderItems { get; set; }
        public DbSet<KontaktCard> KontaktCards { get; set; }
        public DbSet<ImpressumContent> ImpressumContents { get; set; }
        public DbSet<DatenschutzContent> DatenschutzContents { get; set; }

        public DbSet<UrheberrechtContent> UrheberechtContents { get; set; }

        public DbSet<MakerSpaceProject> MakerSpaceProjects { get; set; }
        public DbSet<MakerSpaceDescription> MakerSpaceDescriptions { get; set; }
        public DbSet<ContactEmail> ContactEmail { get; set; }


        //------------ StoryBoard section ------------

        public DbSet<Storyboard> Storyboards => Set<Storyboard>();
        public DbSet<Scene> Scenes => Set<Scene>();
        public DbSet<Marker> Markers => Set<Marker>();

        //------------ Lernpfad section ------------
        public DbSet<LernFlow> LernFlows { get; set; }
        public DbSet<LernStep> LernSteps { get; set; }


        public DbSet<RegistrationCode> RegistrationCodes { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Storyboard>()
               .HasMany(s => s.Scenes)
               .WithOne(sc => sc.Storyboard!)
               .HasForeignKey(sc => sc.StoryboardId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Scene>()
                .HasMany(sc => sc.Markers)
                .WithOne(m => m.Scene!)
                .HasForeignKey(m => m.SceneId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Marker>().Property(m => m.X).HasPrecision(6, 4);
            modelBuilder.Entity<Marker>().Property(m => m.Y).HasPrecision(6, 4);

            modelBuilder.Entity<Storyboard>()
        .HasIndex(s => s.PublicId)
        .IsUnique();

            modelBuilder.Entity<UserSecurityState>()
                .HasIndex(u => u.UserId)
                .IsUnique();

            // Lernpfad: relationships and indexes
            modelBuilder.Entity<LernFlow>()
                .HasMany(f => f.Steps)
                .WithOne(s => s.Flow!)
                .HasForeignKey(s => s.LernFlowId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LernFlow>()
                .HasIndex(f => f.PublicId)
                .IsUnique();


            modelBuilder.Entity<ContactEmail>().HasData(
new ContactEmail
{
Id = 1,
Email = "h.seehagen-marx@uni-wuppertal.de"
}
);

            modelBuilder.Entity<MakerSpaceDescription>().HasData(
new MakerSpaceDescription
{
  Id = 1,
  Title = "Willkommen im ToolBar",
  SubTitle = "Kuratierte Links, Impulse und Ressourcen für die Entwicklung von XR-Lehr- und Lernmedien",
  Content = " In unserem digitalen Makerspace findest du kuratierte Links, inspirierende Impulse und praxisnahe Ressourcen rund um die Entwicklung von Extended Reality (XR). Ob du gerade erst einsteigst oder bereits eigene Projekte realisierst – hier bekommst du Zugang zu Tools, Tutorials, Frameworks und Ideen, die dich bei der Umsetzung deiner XR-Vision unterstützen.\r\n\r\n                    Tauche ein in die Welt von Virtual Reality (VR), Augmented Reality (AR) und Mixed Reality (MR) – von ersten Prototypen bis hin zu fortgeschrittenen Anwendungen. Der Makerspace ist dein Startpunkt für Experimente, Austausch und technologische Kreativität.",

}
);

            modelBuilder.Entity<MakerSpaceProject>().HasData(
    new MakerSpaceProject
    {
        Id = 1,
        Title = "3D Printed Prosthetic Hand",
        Tags = "3D Printing, Prosthetics, Open Source",
        Description = "A low-cost 3D printed prosthetic hand designed for children. Fully open-source and customizable.",
        ProjectUrl = "https://example.com/prosthetic-hand"
    },
    new MakerSpaceProject
    {
        Id = 2,
        Title = "Smart Plant Watering System",
        Tags = "IoT, Arduino, Sensors, Plants",
        Description = "An automatic watering system for plants using soil moisture sensors and Arduino.",
        ProjectUrl = "https://example.com/smart-watering"
    },
    new MakerSpaceProject
    {
        Id = 3,
        Title = "DIY Desktop CNC Machine",
        Tags = "CNC, DIY, Fabrication, Open Hardware",
        Description = "Build your own CNC milling machine using affordable components and open hardware designs.",
        ProjectUrl = "https://example.com/desktop-cnc"
    },
    new MakerSpaceProject
    {
        Id = 4,
        Title = "Voice-Controlled Assistant with Raspberry Pi",
        Tags = "Raspberry Pi, Voice Recognition, Python, AI",
        Description = "A smart assistant using Raspberry Pi and voice recognition libraries to execute custom commands.",
        ProjectUrl = "https://example.com/voice-pi"
    }
);



            modelBuilder.Entity<ImpressumContent>().HasData(
new ImpressumContent
{
Id = 1,
Title = "Impressum Einleitung",
SectionType = "Text",
ContentHtml = "Dies ist der Einleitungstext für das Impressum.",
DisplayOrder = 1
},
new ImpressumContent
{
Id = 2,
Title = "Verantwortlich",
SectionType = "Accordion",
ContentHtml = "Name und Anschrift des Verantwortlichen...",
DisplayOrder = 2
}
);

            modelBuilder.Entity<DatenschutzContent>().HasData(
new DatenschutzContent
{
    Id = 1,
    Title = "Datenschutz Einleitung",
    SectionType = "Text",
    ContentHtml = "Dies ist der Einleitungstext für das Impressum.",
    DisplayOrder = 1
},
new DatenschutzContent
{
    Id = 2,
    Title = "Verantwortlich",
    SectionType = "Accordion",
    ContentHtml = "Name und Anschrift des Verantwortlichen...",
    DisplayOrder = 2
}
);


            // Define Foreign Key relationships with explicit delete behavior


            // Seed initial slider images
            modelBuilder.Entity<SliderItem>().HasData(
                new SliderItem { Id = 1, ImageUrl = "/images/FirstSlider1.png",Title = "Virtueller Campus", Description = "Globale Innovation trifft auf nachhaltiges Lernen <br /> erleben Sie immersive digitale Erlebnisse, die den Campus der Zukunft gestalten." },
                new SliderItem { Id = 2, ImageUrl = "/images/FirstSlider2.png", Title = "Virtueller Campus", Description = "Fördern Sie internationale Zusammenarbeit und gestalten Sie die Zukunft  <br /> des digitalen Forschens in einem virtuellen Raum." },
                new SliderItem { Id = 3, ImageUrl = "/images/FirstSlider3.png", Title = "360° BUW Tour", Description = "Tauchen Sie ein in die Welt der BUW  <br /> entdecken Sie die Universität interaktiv in einer virtuellen Rundreise." },
                new SliderItem { Id = 4, ImageUrl = "/images/FirstSlider4.png", Title = "360°-Rundgang – GSA Konvent", Description = "Erkunden Sie die 'Gallery of Walk'  <br /> eine interaktive Reise durch nachhaltige Visualisierungen von Lehre und Forschung." },
                new SliderItem { Id = 5, ImageUrl = "/images/FirstSlider5.png", Title = "360°-Rundgang – GSA Konvent", Description = "Erleben Sie innovative Posterpräsentationen  <br /> ein interaktiver 360°-Rundgang durch die neuesten Forschungsergebnisse." },
                new SliderItem { Id = 6, ImageUrl = "/images/FirstSlider6.png", Title = "Amphibienschutz in virtuellen 3D-Räumen", Description = "Schützen Sie Salamander in einem innovativen virtuellen Game  <br /> ein interaktives Labor zur Rettung gefährdeter Amphibienarten." },
                new SliderItem { Id = 7, ImageUrl = "/images/FirstSlider7.png", Title = "Amphibienschutz in virtuellen 3D-Räumen", Description = "Erforschen Sie den Schutz gefährdeter Amphibien durch forschendes Lernen im virtuellen Labor  <br /> ein einzigartiger Ansatz für Artenschutz." },
                new SliderItem { Id = 8, ImageUrl = "/images/FirstSlider8.png", Title = "BNE OER", Description = "Bildung für nachhaltige Entwicklung durch Open Educational Resources  <br /> Wissen für die Zukunft zugänglich und nachhaltig vermitteln." },
                new SliderItem { Id = 9, ImageUrl = "/images/FirstSlidera9.png", Title = "Virtuelle Mathematik", Description = "Testen Sie innovative Unterrichtsszenarien in virtuellen Lernräumen  <br /> die Mathematik der Zukunft erleben." },
                new SliderItem { Id = 10, ImageUrl = "/images/FirstSlider10.png", Title = "Virtuelle Räume für die Bildung und Forschung", Description = "Erproben Sie didaktische Szenarien  <br /> entwickeln Sie hybride Lehr-Lernszenarien und erforschen Sie die Verbindung zwischen virtuellen und physischen Lernwelten." },
                new SliderItem { Id = 11, ImageUrl = "/images/FirstSlider11.png", Title = "Salamander-KI-Mustererkennungssoftware", Description = "Mit KI zur präzisen Identifikation von Salamandern  <br /> nachhaltig die Forschung und den Schutz gefährdeter Arten vorantreiben." },
                new SliderItem { Id = 12, ImageUrl = "/images/FirstSlider1.png", IsForVirtuellesKlassenzimmer = true, Title = "Virtueller Campus", Description = "Globale Innovation trifft auf nachhaltiges Lernen <br /> erleben Sie immersive digitale Erlebnisse, die den Campus der Zukunft gestalten." }
            );

            // Seed initial slider images
          

            // Seed PortalCards
            modelBuilder.Entity<MitmachenContent>().HasData(
                //new MitmachenContent
                //{
                //    Id = 1,
                //    SectionType = "Header",
                //    Title = "Mitmachen im MediaLab",
                //    Content = "Gemeinsam Ideen entwickeln, Prototypen bauen und Innovationen realisieren.",

                //},
                    new MitmachenContent
                    {
                        Id = 2,
                        SectionType = "Card",
                        Title = "Von der Idee zur Umsetzung: Projekte im MediaLab",
                        Content = "Das MediaLab der Bergischen Universität Wuppertal ist ein inspirierender Makerspace, die kreativen Köpfe aus unterschiedlichen Disziplinen zusammenbringt. Hier haben Studierende, Lehrende und Forschende die Möglichkeit, ihre Ideen in die Praxis umzusetzen, Prototypen zu entwickeln und neue Technologien auszuprobieren. Mit einer gut ausgestatteten Infrastruktur und einem interdisziplinären Ansatz bietet das MediaLab einen idealen Raum, um an zukunftsorientierten Projekten zu arbeiten und gemeinsam innovative Lösungen zu entwickeln.",

                    },
                    new MitmachenContent
                    {
                        Id = 3,
                        SectionType = "Accordion",
                        Title = "Für Lehrende – Ihre Chancen im MediaLab",
                        Content = "Ihre Ideen realisieren: Nutzen Sie das MediaLab, um technologische, kreative oder didaktische Ansätze in Form von Prototypen zu verwirklichen. Von der Skizze zum Modell: Entwickeln Sie konkrete Prototypen, die Ihre Konzepte anschaulich machen und eine Weiterentwicklung erleichtern. Innovative Lösungen testen: Experimentieren Sie mit neuen Ansätzen und Technologien, um ihre Praxistauglichkeit zu evaluieren. Förderprojekte mit Prototypen stärken: Prototypen als Grundlage: Entwickeln Sie funktionale Modelle, die Förderprojekten eine klare und überzeugende Basis bieten. Lehrmethoden modellieren: Erstellen Sie Prototypen für digitale Tools, didaktische Konzepte oder virtuelle Formate. Testen und optimieren: Evaluieren Sie die Wirksamkeit Ihrer Ideen in einer experimentellen Umgebung, bevor sie in der Lehre angewendet werden. Didaktische Konzepte visualisieren: Praxisnah und anschaulich: Entwickeln Sie Prototypen, die komplexe didaktische Ansätze verständlich machen. Gemeinsam gestalten: Arbeiten Sie mit Kolleg*innen, Forschenden und Studierenden zusammen, um Konzepte zu entwickeln, die den Anforderungen der digitalen Transformation gerecht werden.",

                    },
                       new MitmachenContent
                       {
                           Id = 4,
                           SectionType = "Accordion",
                           Title = "Für Studierende – Ihre Chancen im MediaLab",
                           Content = "Praktika und Abschlussarbeiten: Nutzen Sie das MediaLab als Ausgangspunkt für spannende Themen, die Praxis und Wissenschaft verbinden. Entwickeln Sie innovative Lösungen und lassen Sie Ihre Abschlussarbeit Teil eines realen Projekts werden. Seminararbeiten: Arbeiten Sie im Rahmen Ihrer Seminare an praxisnahen Aufgaben, die interdisziplinäre Ansätze fördern und kreative Technologien nutzen. Ihre Arbeit kann Impulse für zukünftige Projekte setzen. Hilfskraftstellen: Engagieren Sie sich als studentische Hilfskraft im MediaLab und unterstützen Sie spannende Projekte. Dabei erweitern Sie Ihre Kenntnisse in einem inspirierenden Umfeld und sammeln wertvolle Praxiserfahrung. Einfach aus Interesse: Haben Sie eine eigene Idee oder möchten Sie Teil eines kreativen Teams sein? Das MediaLab bietet Ihnen Raum, Unterstützung und eine Community, um Ihrer Leidenschaft nachzugehen – auch unabhängig von Ihrem Studium.",

                       }, new MitmachenContent
                       {
                           Id = 5,
                           SectionType = "Accordion",
                           Title = "Für Forschende – Ihre Möglichkeiten im MediaLab",
                           Content = "Interdisziplinäre Projekte initiieren: Starten Sie eigene Forschungsvorhaben, die verschiedene Disziplinen miteinander verbinden. Das MediaLab fördert die Zusammenarbeit zwischen Fachbereichen und schafft Synergien für zukunftsweisende Lösungen. Technologische Innovationen erkunden: Nutzen Sie die Ausstattung und Expertise des MediaLabs, um mit vielfältigen Technologien zu experimentieren und innovative Forschungsansätze zu entwickeln. Förderprojekte realisieren: Das MediaLab bietet Unterstützung bei der Konzeption und Umsetzung von Drittmittelprojekten. Von der Antragstellung bis zur Durchführung – wir begleiten Sie bei jedem Schritt. Forschung sichtbar machen: Präsentieren Sie Ihre Ergebnisse auf dem Projekte-Portal und teilen Sie Ihre Arbeit mit einer breiten Community. Ihre Forschung wird Teil eines Netzwerks, das Innovation und Wissenstransfer fördert. Praxisorientierte Lösungen entwickeln: Arbeiten Sie an anwendungsorientierten Konzepten, die nicht nur in der Wissenschaft, sondern auch in Gesellschaft und Wirtschaft einen Unterschied machen.",

                       }, new MitmachenContent
                       {
                           Id = 6,
                           SectionType = "Accordion",
                           Title = "Wie können Sie mitmachen?",
                           Content = "Eigene Projekte einreichen: Haben Sie eine Idee? Reichen Sie diese ein und setzen Sie sie gemeinsam mit dem MediaLab-Team um. Bestehende Projekte unterstützen: Schließen Sie sich laufenden Projekten an und bringen Sie Ihre Stärken ein. Angebote nutzen: Bewerben Sie sich auf Praktika, Hilfskraftstellen oder nutzen Sie das MediaLab als Basis für Ihre Abschlussarbeit. Jetzt aktiv werden und die digitale Zukunft mitgestalten: Auf Kontakt Verweisen Das MediaLab freut sich auf Ihre Ideen, Ihr Engagement und Ihre Neugier!",

                       }

                );

            modelBuilder.Entity<KontaktCard>().HasData(
             new KontaktCard
             {
                 Id = 1,
                 Funktion = "Leitung MediaLab",
                 Name = "Dr. Heike Seehagen-Marx",
                 KontaktDatenHtml = "Bergische Universität Wuppertal, Zentrum für Informations- und Medienverarbeitung (ZIM), Medienlabor (Leitung)",
                 ImageUrl = "/images/Kontakt/Heike_Seehagen-Marx.jpg",
                 DisplayOrder = 1
             }
         );

            // Seed PortalCards
            modelBuilder.Entity<PortalCard>().HasData(
                new PortalCard
                {
                    Id = 1,
                    Title = "Projekte und Ideen sichtbar machen",
                    Content = "Das MediaLab des ZIM an der Bergischen Universität Wuppertal (BUW) ist ein zentraler Makerspace, der interdisziplinären Lehre und Forschung fördert. Mit unserem neuen Projekte-Portal möchten wir die Vielfalt und den Impact unserer Arbeit sichtbar machen. Das Portal bietet einen umfassenden Überblick über unsere interdisziplinären Projekte und zeigt unser Engagement für Transparenz, Nachhaltigkeit und die aktive Mitgestaltung der digitalen Transformation. Hier erhalten Sie Einblicke in die innovativen Projekte, die durch die Zusammenarbeit von Studierenden, Forschenden und Lehrenden entstehen.",
                   
                },
                new PortalCard
                {
                    Id = 2,
                    Title = "Sichtbarkeit der Netzwerker",
                    Content = "Das MediaLab ist eine zentrale Anlaufstelle für Akteur*innen aus verschiedenen Disziplinen. Das Portal bietet eine sichtbare Plattform, die Kooperationspartner, Projektergebnisse und interdisziplinären Austausch in den Fokus stellt. So entstehen neue Impulse für Zusammenarbeit und Innovation.",
                 
                },
                new PortalCard
                {
                    Id = 3,
                    Title = "Nachhaltige Konzepte für die Zukunft",
                    Content = "Die Projekte im Portal repräsentieren nachhaltige Konzepte und praxisorientierte Innovationen, die über Disziplinen hinauswirken. Sie werden langfristig zugänglich gemacht, weiterentwickelt und in neue Kontexte übertragen, was den Wissenstransfer und die Förderung interdisziplinärer Expertise unterstützten.",
                   
                },
                  new PortalCard
                  {
                      Id = 4,
                      Title = "Projektförderungen im Fokus",
                      Content = "Von Open Educational Resources (OER)-Initiativen bis zu aktuellen Projekten wie „Kollaborativ Biodiversität entdecken“ zeigt das Portal die Vielfalt der MediaLab-Projekte. Diese verdeutlichen, wie innovative Technologien und interdisziplinäre Zusammenarbeit Lehre und Forschung bereichern.",

                  },
                   new PortalCard
                   {
                       Id = 5,
                       Title = "Gemeinsam Zukunft gestalten",
                       Content = "Das Projekte-Portal ist eine zentrale Plattform, die Transparenz, Nachhaltigkeit und die Sichtbarkeit von Netzwerken fördert. Es zeigt, wie das MediaLab als Raum für Co-Creation und kreative Zusammenarbeit die digitale Zukunft der BUW aktiv mitgestaltet. Alle Universitätsangehörigen sind eingeladen, die Vielfalt der Projekte zu entdecken und gemeinsam an neuen Lösungen zu arbeiten.\r\n",

                   }
            );

            // Seed default Urheberrecht content
            modelBuilder.Entity<UrheberrechtContent>().HasData(
new UrheberrechtContent
{
    Id = 1,
    Title = "Datenschutz Einleitung",
    SectionType = "Text",
    ContentHtml = "Dies ist der Einleitungstext für das Impressum.",
    DisplayOrder = 1
},
new UrheberrechtContent
{
    Id = 2,
    Title = "Verantwortlich",
    SectionType = "Accordion",
    ContentHtml = "Name und Anschrift des Verantwortlichen...",
    DisplayOrder = 2
}
);
            // Seed default Leichtesprache content
            modelBuilder.Entity<LeichteSpracheContent>().HasData(new LeichteSpracheContent
            {
                Id = 1,
                ContentHtml = "@{\r\n    ViewData[\"Title\"] = \"Leichtesprache\";\r\n}\r\n\r\n<div class=\"container mt-5\">\r\n    <div class=\"row justify-content-center\">\r\n        <div class=\"col-md-10\">\r\n            <!-- Title -->\r\n            <h2 class=\"mb-4 \" style=\"color:#90bc14\">Projekte im MediaLab (MediaLab-Projekte)</h2>\r\n\r\n            <!-- Introduction -->\r\n            <p class=\"lead\">\r\n                Die Webseite \"MediaLab-Projekte\" präsentiert die Ergebnisse der gemeinsamen Projekte im MediaLab in einer übersichtlichen Projektbibliothek.\r\n            </p>\r\n\r\n            <p class=\"lead\">\r\n                Das MediaLab an der Bergischen Universität Wuppertal ist ein kreativer Raum, in dem Studierende, Lehrende und Forschende ihre Ideen umsetzen, neue Technologien testen und Prototypen entwickeln können.\r\n            </p>\r\n\r\n            <!-- Section: Lehrende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Lehrende – Ihre Chancen im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Ideen umsetzen:</strong> Bringen Sie Ihre Ideen ins MediaLab und setzen Sie sie gemeinsam mit anderen um.</li>\r\n                <li class=\"list-group-item\"><strong>Prototypen entwickeln:</strong> Machen Sie aus Ihren Ideen Modelle, die Ihre Konzepte zeigen und weiterentwickeln.</li>\r\n                <li class=\"list-group-item\"><strong>Neue Lösungen testen:</strong> Probieren Sie neue Technologien und Methoden aus und testen Sie, ob sie gut funktionieren.</li>\r\n                <li class=\"list-group-item\"><strong>Zusammenarbeiten:</strong> Arbeiten Sie mit Studierenden und Kolleg*innen an innovativen Lösungen.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Studierende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Studierende – Ihre Chancen im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Praktika und Abschlussarbeiten:</strong> Nutzen Sie das MediaLab für spannende Themen, die Theorie und Praxis verbinden.</li>\r\n                <li class=\"list-group-item\"><strong>Seminararbeiten:</strong> Arbeiten Sie an praktischen Aufgaben und bringen Sie kreative Ideen ein.</li>\r\n                <li class=\"list-group-item\"><strong>Hilfskraftstellen:</strong> Engagieren Sie sich im MediaLab und sammeln Sie wertvolle Praxiserfahrung.</li>\r\n                <li class=\"list-group-item\"><strong>Eigene Ideen umsetzen:</strong> Haben Sie eine Idee? Nutzen Sie das MediaLab, um Ihre Projekte umzusetzen.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Forschende -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Für Forschende – Ihre Möglichkeiten im MediaLab</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Projekte starten:</strong> Starten Sie eigene Forschung und arbeiten Sie mit anderen Disziplinen zusammen.</li>\r\n                <li class=\"list-group-item\"><strong>Neue Technologien testen:</strong> Nutzen Sie die Ausstattung im MediaLab, um neue Ideen und Technologien auszuprobieren.</li>\r\n                <li class=\"list-group-item\"><strong>Förderprojekte umsetzen:</strong> Holen Sie sich Unterstützung für Projekte mit Fördermitteln.</li>\r\n                <li class=\"list-group-item\"><strong>Forschung teilen:</strong> Stellen Sie Ihre Forschungsergebnisse auf der Webseite vor.</li>\r\n            </ul>\r\n\r\n            <!-- Section: Mitmachen -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Wie können Sie mitmachen?</h2>\r\n            <ul class=\"list-group list-group-flush mb-4\">\r\n                <li class=\"list-group-item\"><strong>Eigene Projekte einreichen:</strong> Haben Sie eine Idee? Reichen Sie sie ein und arbeiten Sie mit uns zusammen.</li>\r\n                <li class=\"list-group-item\"><strong>Bestehende Projekte unterstützen:</strong> Schließen Sie sich laufenden Projekten an und bringen Sie Ihre Stärken ein.</li>\r\n                <li class=\"list-group-item\"><strong>Angebote nutzen:</strong> Bewerben Sie sich auf Praktika, Hilfskraftstellen oder nutzen Sie das MediaLab für Ihre Abschlussarbeit.</li>\r\n                <li class=\"list-group-item\"><strong>Jetzt aktiv werden:</strong> Das MediaLab freut sich auf Ihre Ideen und Ihr Engagement! Kontaktieren Sie uns!</li>\r\n            </ul>\r\n\r\n            <!-- Contact Section -->\r\n            <h2 class=\"mt-5 \" style=\"color:#90bc14\">Kontakt</h2>\r\n            <p class=\"mb-0\">\r\n                <strong>Dr. Heike Seehagen-Marx</strong><br>\r\n                <a href=\"mailto:h.seehagen-marx@uni-wuppertal.de\" class=\"text-decoration-none text-primary\">h.seehagen-marx@uni-wuppertal.de</a>\r\n            </p>\r\n        </div>\r\n    </div>\r\n</div>\r\n"
            });

            // Seed default Übersicht content
            modelBuilder.Entity<UebersichtContent>().HasData(new UebersichtContent
            {
                Id = 1,
                ContentHtml = @"
@{
    ViewData[""Title""] = ""Übersicht"";
}

<div class=""container mt-5"">
    <div class=""row justify-content-center"">
        <div class=""col-md-10"">

            <!-- Title -->
            <h2 class=""mb-4"" style=""color:#90bc14"">Übersicht der Projekte</h2>

            <!-- Introduction -->
            <p class=""lead"">
                Hier finden Sie eine Übersicht über die verschiedenen Projekte im MediaLab und BioVersum.
            </p>

            <p>
                Jedes Projekt ist einzigartig und bietet spannende Einblicke in aktuelle Entwicklungen, Forschungsthemen und kreative Lösungen.
            </p>

            <!-- Sections -->
            <h2 class=""mt-5"" style=""color:#90bc14"">Projektkategorien</h2>
            <ul class=""list-group list-group-flush mb-4"">
                <li class=""list-group-item""><strong>Lehre:</strong> Projekte, die zur Unterstützung von Lehrveranstaltungen entwickelt wurden.</li>
                <li class=""list-group-item""><strong>Forschung:</strong> Forschungsbasierte Projekte zur Entwicklung neuer Erkenntnisse und Methoden.</li>
                <li class=""list-group-item""><strong>Studentische Arbeiten:</strong> Abschlussarbeiten, Seminarprojekte und Praktika.</li>
                <li class=""list-group-item""><strong>Offene Projekte:</strong> Projekte, an denen Studierende und Lehrende gemeinsam arbeiten können.</li>
            </ul>

            <!-- Call to action -->
            <h2 class=""mt-5"" style=""color:#90bc14"">Mitmachen und mehr erfahren</h2>
            <p>
                Wenn Sie mehr über ein Projekt erfahren oder sich beteiligen möchten, wenden Sie sich bitte an das MediaLab-Team.
            </p>

            <!-- Contact Section -->
            <h2 class=""mt-5"" style=""color:#90bc14"">Kontakt</h2>
            <p>
                <strong>Dr. Heike Seehagen-Marx</strong><br>
                <a href=""mailto:h.seehagen-marx@uni-wuppertal.de"" class=""text-decoration-none text-primary"">h.seehagen-marx@uni-wuppertal.de</a>
            </p>

        </div>
    </div>
</div>
"
            });


           

            // Seed default KIBar
//            modelBuilder.Entity<MakerSpaceProject>().HasData(

//    new MakerSpaceProject
//    {
//        Id = 5,
//        Title = "Edu-Coach-Workshop",
//        Tags = "KI, Chat-Assistent",
//        Top = true,
//        ProjectUrl = "https://huggingface.co/chat/assistant/679f38d80b57a15c9d556ba5",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/e7e326fa-0eb6-45c1-b76a-fbe2cf0ff7a8.jpg",
//        Description = "<p>Dieser Chat-Assistent-Workshop bietet Ihnen einen ersten Einstieg, um die grundlegenden Möglichkeiten von KI-gesteuerten Assistenten zu entdecken. Gemeinsam werden wir im Workshop die Funktionen und Potenziale dieser Technologie kennenlernen und erste praktische Erfahrungen sammeln. Um aktiv an der Übung mit dem Edu-Assistenten teilzunehmen und dessen Funktionen zu erkunden, benötigen Sie ein Hugging Face-Konto.</p>",
//        DisplayOrder = 5
//    },

//    new MakerSpaceProject
//    {
//        Id = 6,
//        Title = "Adobe Firefly",
//        Tags = "KI, Bilder",
//        Top = true,
//        ProjectUrl = "https://firefly.adobe.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/848e2db6-9c84-4da1-b336-7c357fed7204.svg",
//        Description = "<p>Ein kreatives Werkzeug, das mithilfe von KI visuelle Inhalte aus einfachen Textbeschreibungen erzeugt. Es unterstützt Designer und Kreative dabei, Bilder, Stile, Texturen und Effekte schneller und unkomplizierter zu gestalten. Um den kreativen Prozess zu verbessern und die Erstellung von Grafiken für Marketing, Design und soziale Medien zu erleichtern.</p>",
//        DisplayOrder = 6
//    },

//    new MakerSpaceProject
//    {
//        Id = 7,
//        Title = "H5P Prüfungsassistent",
//        Tags = "KI, Chat-Assistent",
//        Top = true,
//        ProjectUrl = "https://huggingface.co/chat/assistant/67b2092f40b8f74a387e3878",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/c88ad8cd-97a4-47a3-9131-dba5eea63c39.jpg",
//        Description = "<p>Der H5P-Prüfungsassistent unterstützt die Erstellung von Prüfungsaufgaben: Mit dem H5P-Tool unterstützt er Lehrende effizient bei der Generierung von Multiple-Choice, Single-Choice und weiteren Aufgabentypen.</p>",
//        DisplayOrder = 7
//    },

//    new MakerSpaceProject
//    {
//        Id = 8,
//        Title = "3D Vista",
//        Tags = "360Grad, 3D-Objekte",
//        Top = false,
//        ProjectUrl = "https://www.3dvista.com/de/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/677a76fe-82dd-47ec-8c50-dda3af19a42b.png",
//        Description = "<p>Erleben Sie mit 3DVista die Zukunft der virtuellen Rundgänge: Erstellen Sie beeindruckende 360°-Erlebnisse mit interaktiven Hotspots, dynamischen Panoramen und anpassbaren HDR-Effekten. Tauchen Sie ein in immersive 3D-Welten für Messen, Schulungen und mehr.</p>",
//        DisplayOrder = 8
//    },

//    new MakerSpaceProject
//    {
//        Id = 9,
//        Title = "ZUM - Inhaltstypen",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://deutsch-lernen.zum.de/wiki/Kategorie:H5P-Inhaltstypen",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/11a00195-7317-4277-8a1e-fcd3143ad018.png",
//        Description = "<p>Entdecken Sie die Vielfalt der H5P-Inhaltstypen und tauchen Sie ein in eine Welt interaktiver Lernmöglichkeiten.</p>",
//        DisplayOrder = 9
//    },

//    new MakerSpaceProject
//    {
//        Id = 10,
//        Title = "Zedis.digital-H5P",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://zebis.digital/start/67D3PW",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/4c3143c0-7cfc-4d0c-8e06-a756f8437e86.png",
//        Description = "<p>Zusammenstellung mit vielen Beispielen zu fast allen H5P Inhaltstypen.</p>",
//        DisplayOrder = 10
//    },

//    new MakerSpaceProject
//    {
//        Id = 11,
//        Title = "Unity",
//        Tags = "Unity, 3D-Objekte, 3D-Räume",
//        Top = false,
//        ProjectUrl = "https://unity.com/de",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/284b8494-1d39-4485-bf91-e5e1bb03409b.png",
//        Description = "<p>Tauchen Sie ein in die Welt der Virtual Reality mit Unity und der XR API. Von VR-Spielen bis hin zu interaktiven Simulationen ist alles möglich. Erfahren Sie, wie Sie mit fundierten Kenntnissen in Unity, C# und Game-Design beeindruckende VR-Inhalte für verschiedene Hardware erstellen können.</p>",
//        DisplayOrder = 11
//    },

//    new MakerSpaceProject
//    {
//        Id = 12,
//        Title = "Aicolors",
//        Tags = "KI, Color",
//        Top = true,
//        ProjectUrl = "https://aicolors.co/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://www.bairesdev.com/tools/ai-colors/ai-colors-assets/bairesdev-logo.svg",
//        Description = "<p>AIColors. co ist ein KI-gestützter Farbpalettengenerator, mit dem Nutzer mühelos einzigartige und ansprechende Farbpaletten für ihre Projekte erstellen. Die künstliche Intelligenz wandelt Texteingaben in individuelle Farbschemata um, die sich flexibel anpassen und visualisieren lassen. Designer, Künstler und Entwickler finden so schneller harmonische Farben für ihre Arbeiten.</p>",
//        DisplayOrder = 12
//    },

//    new MakerSpaceProject
//    {
//        Id = 13,
//        Title = "ThingLink",
//        Tags = "360Grad",
//        Top = false,
//        ProjectUrl = "https://www.thinglink.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/94781c6d-d78c-482c-9d76-4b339b348e75.svg",
//        Description = "<p>Mit ThingLink verwandeln Sie statische Bilder und Videos in interaktive Erlebnisse - dank zahlreicher Vorlagen und Layouts, die individuell angepasst werden können. Entdecken Sie die Möglichkeiten, Ihre Medieninhalte auf ein neues Level zu heben.</p>",
//        DisplayOrder = 13
//    },

//    new MakerSpaceProject
//    {
//        Id = 14,
//        Title = "Themenkomplex H5P",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://www.bycs.de/themenkomplex/lernplattform/h5p/index.html",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/2efe06f3-ef59-4be4-8ff2-522bc6e26993.svg",
//        Description = "<p>Multimediale Elemente, automatisierte Rückmeldungen und individuelle Interaktionen für ein maßgeschneidertes Lernerlebnis. Formatives Feedback ermöglicht eine kontinuierliche Anpassung des Lernprozesses.</p>",
//        DisplayOrder = 14
//    },

//    new MakerSpaceProject
//    {
//        Id = 16,
//        Title = "Atlasti",
//        Tags = "KI, Forschung",
//        Top = true,
//        ProjectUrl = "https://atlasti.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://getlogovector.com/wp-content/uploads/2021/07/atlas-ti-logo-vector.png",
//        Description = "<p>ATLAS. ti, eine bewährte Software für qualitative Datenanalyse, integriert mit Intentional AI Coding einen fortschrittlichen KI-Assistenten. Forschende geben ihre spezifischen Forschungsziele an und erhalten automatisierte Codierungsvorschläge.</p>",
//        DisplayOrder = 16
//    },

//    new MakerSpaceProject
//    {
//        Id = 17,
//        Title = "Photopea",
//        Tags = "Design",
//        Top = false,
//        ProjectUrl = "https://www.photopea.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/21f7a769-62ba-4206-be21-d3840a286e9e.png",
//        Description = "<p>Diese kostenlose, webbasierte Alternative zu Adobe Photoshop bietet umfassende Bildbearbeitungsfunktionen und unterstützt zahlreiche Dateiformate - ganz ohne Installation. Entdecken Sie, wie Sie mit diesem Tool professionelle Grafiken und Fotos direkt im Browser erstellen und optimieren können.</p>",
//        DisplayOrder = 17
//    },

//    new MakerSpaceProject
//    {
//        Id = 18,
//        Title = "Marzipano",
//        Tags = "360Grad",
//        Top = false,
//        ProjectUrl = "https://www.marzipano.net/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/0d03c422-11ed-4ea4-9bae-c3ad3ff2c7ca.png",
//        Description = "<p>Entdecken Sie die innovative Marzipano Web-Applikation: Erstellen Sie beeindruckende 360°-Panorama-Szenen und -Touren mit interaktiven Links und Anmerkungen, exportieren Sie Ihr Projekt als Website und teilen Sie es auf jedem Webbrowser.</p>",
//        DisplayOrder = 18
//    },

//    new MakerSpaceProject
//    {
//        Id = 19,
//        Title = "Lehrerfortbildung-bw",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://lehrerfortbildung-bw.de/st_digital/moodle/02_anleitungen/03trainer/03aktivitaeten/11h5p/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/b7096887-64f1-48aa-a01f-520fc92eb7e8.svg",
//        Description = "<p>Mit H5P können Lehrkräfte in Moodle mühelos interaktive, multimediale Lerninhalte erstellen und so den Unterricht spannender und effektiver gestalten. Die Open-Source-Erweiterung fördert durch intuitive Vorlagen und automatisierte Rückmeldungen den OER-Gedanken und die Gamification im Bildungsbereich.</p>",
//        DisplayOrder = 19
//    },

//    new MakerSpaceProject
//    {
//        Id = 20,
//        Title = "Lumi H5P Desktop Editor",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://lumi.education/en/lumi-h5p-offline-desktop-editor/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/f21b03c0-59f8-439d-ad07-cd7a4f741785.svg",
//        Description = "<p>Lumi H5P Desktop Editor: Erstellen und bearbeiten Sie H5P-Inhalte ganz einfach offline. Verabschieden Sie sich von der Internet-Abhängigkeit mit diesem leistungsstarken und kostenlosen Tool.</p>",
//        DisplayOrder = 20
//    },

//    new MakerSpaceProject
//    {
//        Id = 21,
//        Title = "Lern App Kompass-H5P",
//        Tags = "H5P",
//        Top = false,
//        ProjectUrl = "https://lern-app-kompass.de/h5p-interaktive-book/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/835949a2-6643-485a-8bc2-4315efa4a4d1.png",
//        Description = "<p>Entdecken Sie, wie digitale Medien und das H5P-Framework das Lehren und Lernen revolutionieren können. Erfahren Sie, wie interaktive Inhalte die Wissensvermittlung effektiver gestalten und mehr Flexibilität in der Bildung ermöglichen.</p>",
//        DisplayOrder = 21
//    },

//    new MakerSpaceProject
//    {
//        Id = 22,
//        Title = "brandmark.io",
//        Tags = "KI, Schriften",
//        Top = true,
//        ProjectUrl = "https://brandmark.io/font-generator/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/fa16ad0d-3b65-46fb-b113-f358c7b1c274.png",
//        Description = "<p>Entdecken Sie mit dem Brandmark Font Generator ein innovatives Online-Tool, das Designern ermöglicht, einzigartige Schriftartenkombinationen mithilfe von KI zu erstellen und visuelle Kontraste zu finden.</p>",
//        DisplayOrder = 22
//    },

//    new MakerSpaceProject
//    {
//        Id = 23,
//        Title = "H5P Tutorials",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://www.media-data.org/h5p-tutorials/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/2419c1b1-6713-41ff-98e1-2d63b65ec974.png",
//        Description = "<p>Mit H5P gestalten Sie interaktive Lerninhalte im Handumdrehen - unsere Tutorials und Praxisbeispiele zeigen Ihnen, wie es geht. Tauchen Sie ein in die Welt des digitalen Unterrichts und entdecken Sie vielseitige Anwendungsmöglichkeiten!</p>",
//        DisplayOrder = 23
//    },

//    new MakerSpaceProject
//    {
//        Id = 24,
//        Title = "H5P Studio",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://h5pstudio.ecampusontario.ca/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/1a32e23c-8e1b-47e1-a1ba-84926f1a707a.png",
//        Description = "<p>Das eCampusOntario H5P Studio erlaubt interaktive Lerninhalte aus verschiedenen Fachgebieten zu erstellen, zu teilen und zu entdecken. Der Katalog bietet zahlreiche H5P-Aktivitäten, meist unter Creative Commons-Lizenzen. Als gemeinnütziges Kompetenzzentrum unterstützt eCampusOntario das technologiegestützte Lehren und Lernen.</p>",
//        DisplayOrder = 24
//    },

//    new MakerSpaceProject
//    {
//        Id = 25,
//        Title = "Canva",
//        Tags = "KI, Visualisieren, Design ",
//        Top = true,
//        ProjectUrl = "https://www.canva.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/c46eb62a-41e8-46ea-af85-572b74a0d225.svg",
//        Description = "<p>Design-Plattform, die KI-gestützte Funktionen integriert, um die Erstellung von Grafiken, Präsentationen, Social-Media-Posts und anderen visuellen Inhalten zu vereinfachen. Mit KI-Tools wie dem Text-zu-Bild-Generator, Designvorschlägen und automatischer Bildoptimierung können Benutzer schnell und einfach ansprechende Designs erstellen.</p>",
//        DisplayOrder = 25
//    },

//    new MakerSpaceProject
//    {
//        Id = 26,
//        Title = "H5P-SMZ-Stuttgart",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://angebote.smz-stuttgart.de/pluginfile.php/803/mod_resource/content/1/H5P%20ChatGPT%20Prompts.pdf",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/281ca711-5a99-4789-9c2f-f2966ce86059.svg",
//        Description = "<p>Entdecken Sie die vielfältigen Möglichkeiten von H5P-Inhalten im Unterricht. Von Multiple-Choice-Quizzen bis hin zu Lückentexten - erfahren Sie, wie diese interaktiven Tools das Verständnis prüfen und das Lernen bereichern. Tauchen Sie ein in die Welt von H5P und entdecken Sie, wie diese innovativen Aktivitäten den Unterricht revolutionieren können.</p>",
//        DisplayOrder = 26
//    },

//    new MakerSpaceProject
//    {
//        Id = 27,
//        Title = "H5P-Prompt",
//        Tags = "KI, Prompt, H5P",
//        Top = true,
//        ProjectUrl = "https://h5p.org/GPT",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/1f22a05f-25ad-44e0-a203-1ed9f9718941.svg",
//        Description = "<p>Mit Chat GPT und H5P interaktive Inhalte erstellen: Entdecken Sie, wie künstliche Intelligenz und Open-Source-Technologie zusammenkommen, um Quiz und dynamische Zusammenfassungen zu revolutionieren.</p>",
//        DisplayOrder = 27
//    },

//    new MakerSpaceProject
//    {
//        Id = 28,
//        Title = "ChatGPT",
//        Tags = "KI, Schreibprozesse ",
//        Top = true,
//        ProjectUrl = "https://chat.openai.com/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/e/ef/ChatGPT-Logo.svg/330px-ChatGPT-Logo.svg.png",
//        Description = "<p>Ein Chatbot von OpenAI, der menschenähnliche Gespräche führen kann. Er verwendet maschinelles Lernen, um auf Texteingaben zu reagieren und Aufgaben wie Fragen beantworten, Texte generieren oder übersetzen zu übernehmen.</p>",
//        DisplayOrder = 28
//    },

//    new MakerSpaceProject
//    {
//        Id = 29,
//        Title = "H5P-ORG",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://h5p.org/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/f59abb78-b7d9-4508-88d8-8b495339aec6.svg",
//        Description = "<p>H5P revolutioniert die Erstellung interaktiver HTML5-Inhalte: Das kostenlose, quelloffene Framework ermöglicht es, ansprechende Online-Erlebnisse direkt im Browser zu gestalten und nahtlos in CMS und LMS zu integrieren. Mit umfassender Dokumentation und einer aktiven Community bietet H5P eine einfache und flexible Lösung für Entwickler und Nutzer.</p>",
//        DisplayOrder = 29
//    },

//    new MakerSpaceProject
//    {
//        Id = 30,
//        Title = "H5P & Künstliche Intelligenz",
//        Tags = "KI, H5P",
//        Top = true,
//        ProjectUrl = "https://h5p.org/using-ai-to-create-h5p-content",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/535c860c-6763-4152-b60f-0396374155ba.svg",
//        Description = "<p>Die Integration von KI-Tools wie ChatGPT in die Erstellung von H5P-Frageformaten revolutioniert die Gestaltung interaktiver Lerninhalte. Doch nur durch präzise Anweisungen können diese automatisierten Inhalte den Bildungsprozess wirklich bereichern.</p>",
//        DisplayOrder = 30
//    },

//    new MakerSpaceProject
//    {
//        Id = 31,
//        Title = "Colormind",
//        Tags = "KI, Color, Design",
//        Top = true,
//        ProjectUrl = "http://colormind.io/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/5037ec0b-07df-4226-a01f-287768192bec.svg",
//        Description = "<p>Colormind erstellt ein Farbdesign: Mit Deep Learning generiert die KI ästhetische Farbschemata aus Fotos, Filmen und Kunstwerken und bietet Designern eine Lösung für harmonische Farbpaletten.</p>",
//        DisplayOrder = 31
//    },

//    new MakerSpaceProject
//    {
//        Id = 32,
//        Title = "Windsurf",
//        Tags = "KI, Programmieren ",
//        Top = true,
//        ProjectUrl = "https://windsurf.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/d589d7a5-62c1-47df-9934-a6d44a4027e7.svg",
//        Description = "<p>Windsurf, vorher unter Codeium bekannt, ist ein Tool, das Entwicklern mit intelligenten Codevorschlägen und -ergänzungen hilft, effizienter zu arbeiten.</p>",
//        DisplayOrder = 32
//    },

//    new MakerSpaceProject
//    {
//        Id = 33,
//        Title = "H5P mit KI erstellen",
//        Tags = "KI, H5P",
//        Top = true,
//        ProjectUrl = "https://mpz-landkreis-leipzig.de/h5p-mit-ki-erstellen/#H5P-Werkzeuge,-die-eine-problemlose-Erstellung-per-KI-erm%C3%B6glichen",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/b3f9a834-3142-49f6-85c9-dba2936f4b3d.png",
//        Description = "<p>Das Medienpädagogische Zentrum Landkreis Leipzig demonstriert, wie Lehrkräfte H5P und KI verbinden, um interaktive Lernmaterialien zu gestalten. Der Artikel erläutert die Grundlagen von H5P, KI und Prompts, zeigt deren Einsatz für Quiz, Kreuzworträtsel und Lückentexte und erklärt die Erstellung von Bildern und 360°-Panoramen. Außerdem wird beschrieben, wie man mit KI eine H5P-GameMap-Story entwickelt. Ziel ist es, Lehrkräfte dabei zu unterstützen, motivierende Lernangebote zu schaffen.</p>",
//        DisplayOrder = 33
//    },

//    new MakerSpaceProject
//    {
//        Id = 34,
//        Title = "H5P Interaktive Übungen",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://www.schule-bw.de/themen-und-impulse/medienbildung/interaktiv/index.html",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://www.schule-bw.de/++theme++plonetheme.lbs/img/lbs-logo-neu2.png",
//        Description = "<p>Entdecken Sie interaktive Lernübungen auf dem Landesbildungsserver, erstellt mit dem innovativen Programmpaket H5P. Erfahren Sie mehr in unserer Einführung zu H5P.</p>",
//        DisplayOrder = 34
//    },

//    new MakerSpaceProject
//    {
//        Id = 35,
//        Title = "H5P-Elementtypen",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://www.xn--martina-rter-llb.de/lernen-elearing/h5p-elementtypen-mit-lumi-offline-erstellen/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/fce863ac-ff40-4a55-94b8-19f310cc6215.jpg",
//        Description = "<p>Die Software, die interaktive Inhalte zum Leben erweckt. Von interaktiven Videos bis hin zu 360°-Touren - mit H5P ist alles möglich. Tauchen Sie ein in die Welt der kreativen Möglichkeiten!</p>",
//        DisplayOrder = 35
//    },

//    new MakerSpaceProject
//    {
//        Id = 36,
//        Title = "Connected Papers",
//        Tags = "KI, Forschung ",
//        Top = true,
//        ProjectUrl = "https://www.connectedpapers.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/017c36fc-50aa-4a62-86c2-13e863e44a1a.png",
//        Description = "<p>Visualisiert Verbindungen zwischen wissenschaftlichen Arbeiten und zeigt relevante Publikationen an.</p>",
//        DisplayOrder = 36
//    },

//    new MakerSpaceProject
//    {
//        Id = 37,
//        Title = "Excalidraw",
//        Tags = "Design",
//        Top = false,
//        ProjectUrl = "https://draw.kits.blog/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/5157b533-7fa1-4c5a-b905-5e1e8cb1c9c0.svg",
//        Description = "<p>Mit Draw. kit wird das Zeichnen im Internet zum Kinderspiel. Entdecken Sie eine Vielzahl von Werkzeugen und Optionen, um Ihre Ideen in beeindruckende Zeichnungen umzusetzen. Pinsel, Stifte, Formen und mehr stehen Ihnen zur Verfügung, um Ihre Kreationen zu perfektionieren.</p>",
//        DisplayOrder = 37
//    },

//    new MakerSpaceProject
//    {
//        Id = 38,
//        Title = "DiLerH5P",
//        Tags = "H5P",
//        Top = true,
//        ProjectUrl = "https://www.digitale-lernumgebung.de/h5p.html",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/18f92eb7-20d4-4296-a8fe-804b30590701.png",
//        Description = "<p>Verschiedene H5P-basierte Aufgabentypen und Beispiele im Überblick.</p>",
//        DisplayOrder = 38
//    },

//    new MakerSpaceProject
//    {
//        Id = 39,
//        Title = "Consensus",
//        Tags = "KI, Forschung ",
//        Top = true,
//        ProjectUrl = "https://consensus.app/search/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/2c153f4e-297f-4d97-8d9e-6ebe0501c6d9.svg",
//        Description = "<p>Consensus ist eine KI-gestützte Plattform zur schnellen Extraktion von Ergebnissen und Schlussfolgerungen aus wissenschaftlichen Studien.</p>",
//        DisplayOrder = 39
//    },

//    new MakerSpaceProject
//    {
//        Id = 40,
//        Title = "Coursebox",
//        Tags = "KI, Lehre planen",
//        Top = true,
//        ProjectUrl = "https://www.coursebox.ai/de",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://cdn.prod.website-files.com/65e9bdf0fae79e05e213200c/660fe39debd7cc7092e09609_Untitled%20design.webp",
//        Description = "<p>Plattform zur Erstellung vollständiger Kursstrukturen mit Lektionen, Zielen und Quizfragen auf Basis weniger Stichworte. Ermöglicht Nutzung von Multimedia-Inhalten, individuelle Anpassung und Vorschaufunktionen zur Verbesserung des Lernerlebnisses vor der Veröffentlichung. Bietet kostenlose Pläne und flexible Optionen für effektive Kurserstellung.</p>",
//        DisplayOrder = 40
//    },

//    new MakerSpaceProject
//    {
//        Id = 41,
//        Title = "Character Creator",
//        Tags = "Avatare, 3D-Objekte, Game",
//        Top = false,
//        ProjectUrl = "https://www.reallusion.com/de/character-creator/digital-human-skin-morph.html",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/1b4b62cb-e363-499d-934f-f3cb05b41e2d.svg",
//        Description = "<p>Die virtuelle Revolution: Mit Character Creator von Reallusion zum perfekten 3D-Charakter. Künstler, Designer und Entwickler aufgepasst - diese Software lässt keine Wünsche offen. Erfahren Sie, wie Sie mit unzähligen Anpassungsmöglichkeiten realistische Figuren zum Leben erwecken. Werfen Sie einen Blick hinter die Kulissen der digitalen Kreativität.</p>",
//        DisplayOrder = 41
//    },

//    new MakerSpaceProject
//    {
//        Id = 42,
//        Title = "Blender",
//        Tags = "Blender, 3D-Objekte",
//        Top = false,
//        ProjectUrl = "https://www.blender.org/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3c/Logo_Blender.svg/768px-Logo_Blender.svg.png",
//        Description = "<p>Mit der Applikation Blender können 3D-Grafiken, Modelle und Animationen erstellt und in verschiedenen Medien, wie z.B. Animationsfilmen und Spielen, eingebunden werden.</p>",
//        DisplayOrder = 42
//    },

//    new MakerSpaceProject
//    {
//        Id = 43,
//        Title = "DALL-E",
//        Tags = "KI, Bilder ",
//        Top = true,
//        ProjectUrl = "https://openai.com/dall-e",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/2583cec8-494d-4271-bf21-67dd3f24b6c9.png",
//        Description = "<p>KI-basierte Bildgenerierung, die detaillierte Bilder aus Textbeschreibungen erzeugt. Unterstützt komplexe Szenen und künstlerische Stile. Ideal für visuelle Konzepte und kreative Projekte.</p>",
//        DisplayOrder = 43
//    },

//    new MakerSpaceProject
//    {
//        Id = 44,
//        Title = "Barrierefreiheit von H5P",
//        Tags = "H5P, Barrierefreiheit",
//        Top = true,
//        ProjectUrl = "https://help.itc.rwth-aachen.de/service/8d9eb2f36eea4fcaa9abd0e1ca008b22/article/80a669e8423d40fcb26d41e4377b6586/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/0d1fde93-41dc-43b5-9984-dd68dd1747f1.png",
//        Description = "<p>Das Kompetenzzentrum digitale Barrierefreiheit.nrw hat H5P-Inhaltstypen auf Barrierefreiheit getestet und zeigt: Kein Typ ist uneingeschränkt barrierefrei. Die detaillierten Ergebnisse bieten Lehrenden wertvolle Entscheidungshilfen.</p>",
//        DisplayOrder = 44
//    },

//    new MakerSpaceProject
//    {
//        Id = 45,
//        Title = "DeepL",
//        Tags = "KI, Übersetzungen ",
//        Top = true,
//        ProjectUrl = "https://www.deepl.com/translator",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/5534b156-b26f-42b1-8f01-24fabee27297.svg",
//        Description = "<p>Präzise Übersetzungen mit Kontextverständnis für über 30 Sprachen. Besonders geeignet für Fachtexte und idiomatische Ausdrücke. Integriert Dokumentenübersetzung und Glossary-Funktion.</p>",
//        DisplayOrder = 45
//    },

//    new MakerSpaceProject
//    {
//        Id = 46,
//        Title = "Barrierefreiheit und H5P",
//        Tags = "H5P, Barrierefreiheit",
//        Top = true,
//        ProjectUrl = "https://barrierefreiheit.dh.nrw/tests/ergebnisse/h5p",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/9aec4e91-5b3d-470d-a74e-8e426878e096.png",
//        Description = "<p>Entdecken Sie, wie H5P Lehrende, eLearning-Verantwortliche und Studierende unterstützt und welche Rolle Barrierefreiheit und Usability spielen. Es bietet einen Blick hinter die Kulissen des Kompetenzzentrums, das die Zukunft des digitalen Lernens gestaltet.</p>",
//        DisplayOrder = 46
//    },

//    new MakerSpaceProject
//    {
//        Id = 47,
//        Title = "BNE OER: Bildung für nachhaltige Entwicklung",
//        Tags = "BNE",
//        Top = false,
//        ProjectUrl = "https://bne-oer.de/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/952392da-d884-43b2-a66f-b6523929bab0.png",
//        Description = "<p>Das BNE OER-Projekt bietet digitale Lerneinheiten zur Bildung für nachhaltige Entwicklung (BNE) als Open Educational Resources (OER) an. Sie unterstützen Pädagoginnen dabei, Schülerinnen für nachhaltiges Handeln zu befähigen. Die Materialien richten sich an Studierende der Frühpädagogik und angehende Lehrkräfte. Entstanden sind diese im Rahmen verschiedener Projekte, die auf der Webseite vorgestellt werden.</p>",
//        DisplayOrder = 47
//    },

//    new MakerSpaceProject
//    {
//        Id = 48,
//        Title = "Whimsical",
//        Tags = "KI, Visualisieren",
//        Top = true,
//        ProjectUrl = "https://whimsical.com/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/508810bc-bbab-48c7-9d86-370bfcd06778.webp",
//        Description = "<p>Whimsical ist ein kollaboratives Online-Tool für visuelles Denken. Es ermöglicht Teams gemeinsam Mindmaps, Flussdiagramme, Wireframes, Sticky Notes und Dokumente zu erstellen. Zudem zeichnet es sich durch seine einfache Bedienung, intuitive Oberfläche und schnelle Visualisierungsideen aus - ideal für Brainstorming, Projektplanung.</p>",
//        DisplayOrder = 48
//    },

//    new MakerSpaceProject
//    {
//        Id = 49,
//        Title = "Envato",
//        Tags = "KI, Color ",
//        Top = false,
//        ProjectUrl = "https://elements.envato.com/de/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/28a0e1bd-b8e9-4424-909a-84a858752ac0.svg",
//        Description = "<p>Envato vorher unter Deblank bekannt, optimiert Kreativität: KI-Designwerkzeuge und individuelle Farbpaletten begleiten Designer.</p>",
//        DisplayOrder = 49
//    },

//    new MakerSpaceProject
//    {
//        Id = 50,
//        Title = "Veed.io",
//        Tags = "KI, Audio, Sound, Video",
//        Top = true,
//        ProjectUrl = "https://www.veed.io/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/9e587792-84ee-463e-be45-008921235782.png",
//        Description = "<p>Audio- und Video-Editor, der auch Transkriptionen und Untertitel erstellen kann.</p>",
//        DisplayOrder = 50
//    },

//    new MakerSpaceProject
//    {
//        Id = 51,
//        Title = "Turnitin Similarity",
//        Tags = "KI, Plagiatprüfung",
//        Top = true,
//        ProjectUrl = "https://www.turnitin.de/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/b40380f9-4226-4ab8-b41c-9578dcd1b0da.svg",
//        Description = "<p>Eine kostenpflichtige Plagiatsprüfungssoftware für Bildungseinrichtungen. Sie vergleicht eingereichte Arbeiten mit einer umfassenden Datenbank, um Übereinstimmungen zu erkennen.</p>",
//        DisplayOrder = 51
//    },

//    new MakerSpaceProject
//    {
//        Id = 52,
//        Title = "Deepai.org",
//        Tags = "KI, Bilder, Video, Musik, 3D-Objekte",
//        Top = false,
//        ProjectUrl = "https://deepai.org/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/f2a76865-9e1f-49dc-8acb-83bc6574b0f0.svg",
//        Description = "<p>DeepAI bietet eine Vielzahl von innovativen KI-Tools. Von einem KI-basierten Chatbot über die Generierung von Bildern und Musik bis hin zur Erstellung von 3D-Modellen - die Möglichkeiten scheinen grenzenlos. Erfahren Sie, wie DeepAI die Grenzen des Machbaren neu definiert und die Kreativität auf ein neues Level hebt.</p>",
//        DisplayOrder = 52
//    },

//    new MakerSpaceProject
//    {
//        Id = 53,
//        Title = "Trickle AI",
//        Tags = "KI, Programmieren",
//        Top = false,
//        ProjectUrl = "https://www.trickle.so/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/e0642455-875c-41d5-a201-afb3a4ed6599.svg",
//        Description = "<p>Mit Trickle AI können Sie ohne Programmierkenntnisse beeindruckende Websites und KI-gestützte Anwendungen erstellen - dank benutzerfreundlicher Oberfläche und integrierter Designvorlagen. Entdecken Sie, wie einfach digitale Innovation sein kann.</p>",
//        DisplayOrder = 53
//    },

//    new MakerSpaceProject
//    {
//        Id = 54,
//        Title = "Deep Dream Generator",
//        Tags = "KI, Bilder",
//        Top = false,
//        ProjectUrl = "https://deepdreamgenerator.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/61d3aa04-0d94-43eb-bc9f-2e994a24b3dc.svg",
//        Description = "<p>Die App ist eine KI-gestützte Plattform, die Bilder in traumhafte, surreal-verzerrte Kunstwerke verwandelt, indem sie Muster und Strukturen verstärkt.</p>",
//        DisplayOrder = 54
//    },

//    new MakerSpaceProject
//    {
//        Id = 55,
//        Title = "DeepL Write",
//        Tags = "KI, Textanalyse, Schreibprozesse ",
//        Top = true,
//        ProjectUrl = "https://www.deepl.com/de/write",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/309bf82f-35cd-420a-8ddf-ab669d3c726f.svg",
//        Description = "<p>Textoptimierung, die über die bloße Korrektur von Rechtschreib- und Grammatikfehlern hinausgeht und gezielte stilistische Verbesserungsvorschläge bietet.</p>",
//        DisplayOrder = 55
//    },

//    new MakerSpaceProject
//    {
//        Id = 56,
//        Title = "Stable Diffusion",
//        Tags = "KI, Bilder",
//        Top = false,
//        ProjectUrl = "https://stability.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/996d450e-7a51-459a-bcb8-e6d9f1705adc.png",
//        Description = "<p>Die Stable Diffusion-KI ermöglicht eine Open-Source-Bildgenerierung mit lokaler Installation. Außerdem bietet sie eine detaillierte Kontrolle über Generierungsparameter und Erweiterungen durch Community-Modelle.</p>",
//        DisplayOrder = 56
//    },

//    new MakerSpaceProject
//    {
//        Id = 57,
//        Title = "DokuMet QDA/AI",
//        Tags = "KI, Forschung ",
//        Top = false,
//        ProjectUrl = "https://dokumet.de/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/cbb03917-f498-4a58-99d5-55208a5bbfcc.png",
//        Description = "<p>DokuMet-AI, ein speziell für die dokumentarische Methode entwickeltes Tool, unterstützt den qualitativen Forschungsprozess mit künstlicher Intelligenz. Es hilft Forschenden, Textsequenzen zu interpretieren und tiefere Bedeutungsstrukturen im Datenmaterial zu erkennen.</p>",
//        DisplayOrder = 57
//    },

//    new MakerSpaceProject
//    {
//        Id = 58,
//        Title = "Soundraw",
//        Tags = "KI, Sound",
//        Top = false,
//        ProjectUrl = "https://soundraw.io/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/d3dd6746-3d32-44b7-b04c-63ea73f9eb00.webp",
//        Description = "<p>Soundraw ist eine KI-gestützte Musikplattform, mit der Nutzer individuelle, lizenzfreie Musikstücke für Videos, Podcasts und andere kreative Projekte generieren können. Durch Auswahl von Stil, Stimmung, Länge und Instrumenten ermöglicht Soundraw personalisierte Kompositionen, die sich flexibel anpassen und nahtlos in digitale Inhalte integrieren lassen.</p>",
//        DisplayOrder = 58
//    },

//    new MakerSpaceProject
//    {
//        Id = 59,
//        Title = "Scribbr",
//        Tags = "KI, Plagiatprüfung",
//        Top = true,
//        ProjectUrl = "https://www.scribbr.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/cc2672b1-d23a-44bf-ad0f-4ae16e54ef1e.svg",
//        Description = "<p>Neben Lektorat und Korrekturlesen bietet Scribbir auch eine Plagiatsprüfung an. Der Dienst vergleicht Arbeiten mit einer Datenbank von über 99 Milliarden Quellen und zeigt Ähnlichkeitsanteile sowie entsprechende Textstellen an.</p>",
//        DisplayOrder = 59
//    },

//    new MakerSpaceProject
//    {
//        Id = 60,
//        Title = "ElevenLabs",
//        Tags = "KI, Sprachgeneratoren",
//        Top = true,
//        ProjectUrl = "https://elevenlabs.io/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://eleven-public-cdn.elevenlabs.io/payloadcms/9trrmnj2sj8-logo-logo.svg",
//        Description = "<p>ElevenLabs ist ein KI-Werkzeug, das synthetischen&nbsp;Stimmen auf ein höheres Level bringt.&nbsp; Es eröffnet neue Möglichkeiten in der Welt der Sprachsynthese, indem es Stimmen erschafft, die so realistisch und natürlich klingen, dass sie kaum von echten Menschen zu unterscheiden sind.</p>",
//        DisplayOrder = 60
//    },

//    new MakerSpaceProject
//    {
//        Id = 61,
//        Title = "SciSpace",
//        Tags = "KI, Forschung, Zitationen",
//        Top = true,
//        ProjectUrl = "https://scispace.com/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/dfebd40f-28a8-4f4c-9c7b-f116633f4840.svg",
//        Description = "<p>SciSpace ist eine intelligente Plattform zur Literaturverwaltung, die speziell für die kollaborative Forschungsarbeit entwickelt wurde - ideal, um wissenschaftliche Quellen effizient zu organisieren, zu analysieren und im Team zu nutzen.</p>",
//        DisplayOrder = 61
//    },

//    new MakerSpaceProject
//    {
//        Id = 62,
//        Title = "Research Rabbit",
//        Tags = "KI, Forschung",
//        Top = true,
//        ProjectUrl = "https://www.researchrabbit.ai/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/8e7ddd05-f7c9-412a-8ee4-b8b64969b518.png",
//        Description = "<p>Ein Tool zur Recherche und Entdeckung für Forscher. Hilft bei der Suche nach ähnlichen Arbeiten und dem Aufbau von Publikationsnetzwerken.</p>",
//        DisplayOrder = 62
//    },

//    new MakerSpaceProject
//    {
//        Id = 63,
//        Title = "Elicit",
//        Tags = "KI, Forschung, Literaturrecherche ",
//        Top = true,
//        ProjectUrl = "https://elicit.com/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/06b5cad6-f9ad-4d4f-8c79-62aa7dc85582.svg",
//        Description = "<p>Ellicit führt, mittels KI-Modellen, automatisierte Literaturrecherche und Extraktion relevanter Studien durch.&nbsp;</p>",
//        DisplayOrder = 63
//    },

//    new MakerSpaceProject
//    {
//        Id = 64,
//        Title = "Gamma",
//        Tags = "KI, Visualisieren",
//        Top = true,
//        ProjectUrl = "https://gamma.app/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/16/GAMMA_Logo.svg/681px-GAMMA_Logo.svg.png?20240929072414",
//        Description = "<p>Gamma ist eine KI-gestützte Plattform, mit der Nutzer Präsentationen, Dokumente und Websites schnell erstellen können- ganz ohne Design- oder Programmierkenntnisse. Die intuitive Oberfläche erlaubt es, interaktive Elemente wie Galerien, Videos und eingebettete Inhalte einzufügen.</p>",
//        DisplayOrder = 64
//    },

//    new MakerSpaceProject
//    {
//        Id = 65,
//        Title = "GitHub Copilot",
//        Tags = "KI, Programmieren ",
//        Top = true,
//        ProjectUrl = "https://github.com/features/copilot",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/7c4451b2-1131-41ab-a8c5-ae61585b0fe9.png",
//        Description = "<p>Ein Tool, das von GitHub in Zusammenarbeit mit OpenAI entwickelt wurde. Es fungiert als intelligenter Code-Editor, der Entwicklern hilft, Code schneller zu schreiben, indem es automatisch Code-Vorschläge basierend auf dem aktuellen Kontext im Editor macht.</p>",
//        DisplayOrder = 65
//    },

//    new MakerSpaceProject
//    {
//        Id = 66,
//        Title = "Google AI Studio",
//        Tags = "KI, Schreibprozesse ",
//        Top = true,
//        ProjectUrl = "https://aistudio.google.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/6e3e0887-4813-45b2-9075-716e5b25c865.png",
//        Description = "<p>Die App bietet Tools zur Modelloptimierung und -überwachung, die bei der Erstellung von Lernmaterialien, Textgenerierung und der Durchführung von Forschungsprojekten hilfreich sind. Sie kann in Bereichen wie Schreiben, Wissenserwerb und Forschung eingesetzt werden, um Projekte und interaktive Lernanwendungen zu entwickeln.</p>",
//        DisplayOrder = 66
//    },

//    new MakerSpaceProject
//    {
//        Id = 67,
//        Title = "Google Scholar",
//        Tags = "KI, Forschung, Literaturrecherche ",
//        Top = true,
//        ProjectUrl = "https://scholar.google.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://scholar.google.com/intl/de/scholar/images/2x/scholar_logo_64dp.png",
//        Description = "<p>Umfassende Suchmaschine für wissenschaftliche Literatur verschiedener Disziplinen und Quellen.</p>",
//        DisplayOrder = 67
//    },

//    new MakerSpaceProject
//    {
//        Id = 68,
//        Title = "Simple Naturgegenstände (Low-Poly)",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/8610a88c-bcfa-4ae6-817c-cbaad69d25b9.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine charmante Low-Poly-Waldszene mit stilisierten Bäumen, Felsen, Grasflächen und natürlichen Elementen wie Baumstümpfen und Büschen - ideal für all Ihre Projekte!</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 68
//    },

//    new MakerSpaceProject
//    {
//        Id = 69,
//        Title = "Hailuo AI",
//        Tags = "KI, Video",
//        Top = false,
//        ProjectUrl = "https://hailuoai.video/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/d52ec6b3-eecc-4308-b352-b961a439a286.png",
//        Description = "<p>Hailuo AI ist ein KI-Tool, dass aus multimodale und generative Eingaben Videos erstellt.</p>",
//        DisplayOrder = 69
//    },

//    new MakerSpaceProject
//    {
//        Id = 70,
//        Title = "Huemint",
//        Tags = "KI, Design",
//        Top = false,
//        ProjectUrl = "https://huemint.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/154a2ae6-51be-4bb0-ae2b-93e3221ee241.png",
//        Description = "<p>Mit Huemint bietet eine Ki für das Design: Das Tool erstellt einzigartige Farbpaletten und ermöglicht es Designern, mit einem Schieberegler die Kreativität ihrer Farbkombinationen zu steuern.</p>",
//        DisplayOrder = 70
//    },

//    new MakerSpaceProject
//    {
//        Id = 71,
//        Title = "Hugging Face Chat",
//        Tags = "KI, Feedback, Schreibprozesse",
//        Top = false,
//        ProjectUrl = "https://huggingface.co/chat/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/f5628ed1-6fbd-476e-9cc6-1fa22d80bf5f.svg",
//        Description = "<p>Mit KI-Chatbot-Modellen kannst du deinen eigenen KI-Chat-Assistenten erstellen. Diese Modelle unterstützen Textgenerierung, interaktive Gespräche und die Möglichkeit, Feedback von Nutzern zu sammeln, indem sie auf natürliche Sprache reagieren. Sie können auch für Rollenspiele, Schreibprozesse und Feedback-Sammlung genutzt werden, um dynamische Interaktionen zu fördern und kontinuierliche Verbesserungen im kreativen oder akademischen Schreiben zu ermöglichen.</p>",
//        DisplayOrder = 71
//    },

//    new MakerSpaceProject
//    {
//        Id = 72,
//        Title = "AllSky Free",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/sky/allsky-free-10-sky-skybox-set-146014",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/04f13b36-8dda-4914-b18f-2d6f8e9fc510.webp",
//        Description = "<p>Mit AllSky Free verleihst du deinen Projekten eine realistische und atmosphärische Himmelskulisse. Das Asset-Pack umfasst zehn hochwertige Skyboxen, die unterschiedliche Tageszeiten und Wetterbedingungen darstellen - von klaren, sonnigen Mittagen bis hin zu dramatischen Sonnenuntergängen und bewölkten Himmeln.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 72
//    },

//    new MakerSpaceProject
//    {
//        Id = 73,
//        Title = "Ideogram.ai",
//        Tags = "KI, Prompt-Repository, Bilder ",
//        Top = false,
//        ProjectUrl = "https://ideogram.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/c0660b95-e69c-4c1e-8e99-df19fb7ba438.png",
//        Description = "<p>Eine Plattform mit intuitiver Benutzeroberfläche zur Anpassung von Stilen und Designs. Es ermöglicht die Umsetzung kreativer Ideen durch die Eingabe von Prompts aus Bildgeneratoren.</p>",
//        DisplayOrder = 73
//    },

//    new MakerSpaceProject
//    {
//        Id = 74,
//        Title = "Immersity.ai",
//        Tags = "KI, Video ",
//        Top = false,
//        ProjectUrl = "https://www.immersity.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/a598d75d-c36b-4110-b1cd-1ab9fabb3d26.svg",
//        Description = "<p>Immersity AI verwandelt mit KI 2D-Bilder und -Videos in fbewegende 3D-Erlebnisse. Nutzer können ihre Inhalte in 3D-Motion-Bilder, 3D-Videos oder 3D-Bilder umwandeln und sie auf XR-Geräten wie Apple Vision Pro und Meta Quest erleben.</p>",
//        DisplayOrder = 74
//    },

//    new MakerSpaceProject
//    {
//        Id = 75,
//        Title = "Jungle AI",
//        Tags = "KI, Quiz, Prüfungen, Feedback",
//        Top = true,
//        ProjectUrl = "https://jungleai.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/b855f517-cda0-47b8-8b6d-724a613a6f69.png",
//        Description = "<p>Jungle Ai ist eine KI-gestützte Plattform, die Vorlesungsfolien in Übungsfragen verwandelt und personalisiertes Feedback bietet. Entdecken Sie, wie diese App den Lernfortschritt verfolgt und individuelle Wiederholungssitzungen anbietet.</p>",
//        DisplayOrder = 75
//    },

//    new MakerSpaceProject
//    {
//        Id = 76,
//        Title = "Future-Skills",
//        Tags = "KI, Lehre planen, Visualisieren",
//        Top = true,
//        ProjectUrl = "https://elp-app.de/apps/future-skills/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/258be312-4e4f-4d44-b7f9-185846bc55fd.png",
//        Description = "<p>Die App Future-Skills vorher bekannt unter KI - Skills, bietet KI-gestützte Einsatzszenarien zur Förderung von Lehre, Lernen und Forschung. Sie ermöglicht die Erkundung und Weiterentwicklung innovativer Ansätze im Bereich der künstlichen Intelligenz und soll Lehrende sowie Forschende mit wertvollen Impulsen für ihre Arbeit unterstützen. Ergänzend dazu bietet eine kuratierte Linkliste weiterführende Informationen zu zentralen Themen der künstlichen Intelligenz.</p>",
//        DisplayOrder = 76
//    },

//    new MakerSpaceProject
//    {
//        Id = 77,
//        Title = "LanguageTool",
//        Tags = "KI, Textanalyse, Schreibprozesse ",
//        Top = true,
//        ProjectUrl = "https://languagetool.org/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/5/54/LanguageTool_Logo_%282018%29.svg/551px-LanguageTool_Logo_%282018%29.svg.png",
//        Description = "<p>Ein Open-Source-Tool, das mehrere Sprachen unterstützt und Fehler in Rechtschreibung, Grammatik und Stil erkennt.</p>",
//        DisplayOrder = 77
//    },

//    new MakerSpaceProject
//    {
//        Id = 78,
//        Title = "3D-Tier-Modelle (Low Poly)",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/characters/animals/quirky-series-free-animals-pack-178235",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/04fb0ee1-1819-47a5-be7a-844a1d3bd2d1.webp",
//        Description = "<p>Dieses Asset-Pack enthält 8 stilisierte 3D-Tiermodelle - perfekt für Spiele, Lernwelten oder stylisierte Naturumgebungen. Zudem ist jedes Tier mit 18 vorgefertigten Animationen ausgestattet. Unter den Tieren gibt es:</p><ul><li>Vogel</li><li>Fisch</li><li>Echse</li><li>Rennmaus</li><li>Schlange</li><li>Affe</li><li>Reh</li><li>Tintenfisch</li></ul><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 78
//    },

//    new MakerSpaceProject
//    {
//        Id = 79,
//        Title = "Looka",
//        Tags = "KI, Logo, Design",
//        Top = false,
//        ProjectUrl = "https://looka.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/565322be-c02a-464d-b5b9-165c2c71ab0e.svg",
//        Description = "<p>Looka ist eine KI-gestützte Plattform, auf der in wenigen Schritten professionelle Logos und umfassende Markenidentitäten zu erstellen sind - ganz ohne Designkenntnisse.</p>",
//        DisplayOrder = 79
//    },

//    new MakerSpaceProject
//    {
//        Id = 80,
//        Title = "Leonardo AI",
//        Tags = "KI, Bilder ",
//        Top = false,
//        ProjectUrl = "https://leonardo.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/7584bd6a-59c5-4b1f-a0a2-ff39c6c928c6.svg",
//        Description = "<p>Leonardo Ai bietet eine professionelle Bildgenerierung mit hoher Kontrolle über Stil und Komposition. Es werden zusätzlich Tools zur Bildverfeinerung und Batch-Erstellung bereitgestellt. Das KI-Tool ist spezialisiert auf kommerzielle Anwendungen.</p>",
//        DisplayOrder = 80
//    },

//    new MakerSpaceProject
//    {
//        Id = 81,
//        Title = "Litmaps",
//        Tags = "KI, Literaturrecherche, Textanalyse ",
//        Top = false,
//        ProjectUrl = "https://www.litmaps.com/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/7d10c227-6576-45b3-821b-893735cf81ac.svg",
//        Description = "<p>Ein KI-gestütztes Werkzeug zur Literaturrecherche, das Forschungszusammenhänge visualisiert und wichtige Arbeiten in einem Fachgebiet identifiziert.</p>",
//        DisplayOrder = 81
//    },

//    new MakerSpaceProject
//    {
//        Id = 83,
//        Title = "MAXQDA",
//        Tags = "KI, Forschung, Textanalyse ",
//        Top = false,
//        ProjectUrl = "https://www.maxqda.com/de/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/72d2ca96-c86c-4925-8f79-565e8afb6f3e.svg",
//        Description = "<p>MAXQDA revolutioniert die qualitative Datenanalyse: Mit der Einführung von AI Assist werden Forschende von komplexen Analyseprozessen entlastet. Wie genau die Integration von künstlicher Intelligenz den Forschungsalltag erleichtert und welche Vorteile sie bietet, erfahren Sie in unserem Artikel.</p>",
//        DisplayOrder = 83
//    },

//    new MakerSpaceProject
//    {
//        Id = 84,
//        Title = "Icons",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/gui/flat-pack-gui-307236",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/2c15b171-4e71-4ead-bd8d-7578b9d76fd2.webp",
//        Description = "<p>Dieses umfangreiche Icon-Asset-Pack bietet 925 vielseitige Symbole zur nahtlosen Integration in deine Projekte.&nbsp;</p>",
//        DisplayOrder = 84
//    },

//    new MakerSpaceProject
//    {
//        Id = 85,
//        Title = "QualCoder",
//        Tags = "KI, Forschung ",
//        Top = false,
//        ProjectUrl = "https://github.com/mdwoicke/QualCoder/blob/ai_integration/README.md",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/748bbd16-a788-4637-9b48-f1648ebc3952.png",
//        Description = "<p>Entdecken Sie die innovative Open-Source-Software QualCoder, die mit KI-gestütztem horizontalen Codieren, die qualitative Datenanalyse revolutioniert. Erfahren Sie, wie Forschende mithilfe interaktiver Interpretationen und vollständiger Kontrolle über die KI-Integration tiefere Einblicke gewinnen können.</p>",
//        DisplayOrder = 85
//    },

//    new MakerSpaceProject
//    {
//        Id = 86,
//        Title = "3D-Schullabor",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/chemistry-lab-items-pack-220212",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/d3ec5cf1-89c8-460a-8ff8-e704372e5cb9.webp",
//        Description = "<p>Dieses Asset-Pack bietet eine Sammlung von 3D-Modellen für ein Schullabor - ideal für die Darstellung von wissenschaftlichen Experimenten und Bildungseinrichtungen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 86
//    },

//    new MakerSpaceProject
//    {
//        Id = 87,
//        Title = "Midjourney",
//        Tags = "KI, Bilder ",
//        Top = false,
//        ProjectUrl = "https://www.midjourney.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/e/e6/Midjourney_Emblem.png",
//        Description = "<p>Midjourney ermöglicht KI-gestützte künstlerisch orientierte Bildgenerierung mit Fokus auf ästhetische Qualität und Experimentierfreude. Die kollaborative Erstellung und Stilmischungen der Bilder werden ebenfalls zur Verfügung gestellt.</p>",
//        DisplayOrder = 87
//    },

//    new MakerSpaceProject
//    {
//        Id = 88,
//        Title = "Miro",
//        Tags = "KI, Visualisieren",
//        Top = true,
//        ProjectUrl = "https://miro.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/9/9c/Mir_company_logo_with_text.tiff/lossless-page1-304px-Mir_company_logo_with_text.tiff.png",
//        Description = "<p>Entdecken Sie mit Miro eine innovative Plattform für kollaboratives Arbeiten an digitalen Whiteboards in Echtzeit, inklusive vielfältiger Vorlagen wie KI-Flussdiagramme und der Möglichkeit für Video- und Telefonkonferenzen in der Vollversion.</p>",
//        DisplayOrder = 88
//    },

//    new MakerSpaceProject
//    {
//        Id = 89,
//        Title = "Mistral",
//        Tags = "KI, Schreibprozesse, Bilder ",
//        Top = true,
//        ProjectUrl = "https://mistral.ai/?utm_source=chatgpt.com",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/de/thumb/1/1c/Mistral_AI_logo.svg/1200px-Mistral_AI_logo.svg.png?20240911082714",
//        Description = "<p>Mit der App werden Open-Source-Modelle zur Text- und Sprachverarbeitung sowie zur Bilderstellung angeboten, die präzise Textgenerierung und Bildkreation ermöglichen.</p>",
//        DisplayOrder = 89
//    },

//    new MakerSpaceProject
//    {
//        Id = 90,
//        Title = "Klassisches 3D-Arbeitszimmer ",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/alchemist-house-112442",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/2ece25b7-2046-4dda-840a-b8aa0b6a2dd9.webp",
//        Description = "<p>Mit diesem Asset-Pack können Sie Ihren Projekten eine lebendige Lernumgebung verleihen. Das Arbeitszimmer ist im altmodischen und klassischen Stil aufgebaut. Ob für Geschichtsstunden oder für die Mathematik, dieses Asset-Pack ist perfekt für das Dastellen eines alten Arbeitszimmers.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 90
//    },

//    new MakerSpaceProject
//    {
//        Id = 91,
//        Title = "Mockitt",
//        Tags = "KI, Visualisieren, Design ",
//        Top = false,
//        ProjectUrl = "https://mockitt.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/b40f7ec3-53bc-4284-93af-5ecbb9f6acf9.svg",
//        Description = "<p>Design, Prototyping und Zusammenarbeit vereint in einem Tool. Wie das die Arbeit von Designern erleichtert und die Benutzererfahrung auf ein neues Level hebt, erfahren Sie hier.</p>",
//        DisplayOrder = 91
//    },

//    new MakerSpaceProject
//    {
//        Id = 92,
//        Title = "Murf.ai",
//        Tags = "KI, Sprachgeneratoren",
//        Top = true,
//        ProjectUrl = "https://murf.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/4d94c48e-aef2-4750-91ac-defffa6d96be.svg",
//        Description = "<p>Eine Lösung, um Text in Sprache umzuwandeln und Videos mit echten Stimmen zu erstellen. Dieses leicht bedienbare KI-Tool ermöglicht es, aus Texten lebendige Sprachaufnahmen zu machen.</p>",
//        DisplayOrder = 92
//    },

//    new MakerSpaceProject
//    {
//        Id = 93,
//        Title = "Boden-Texturen (außen)",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/floors/outdoor-ground-textures-12555",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/ee3784ba-32ac-4d90-b761-f490b5fda910.webp",
//        Description = "<p>Dieses Asset-Pack enthält hochwertige Boden-Texturen für den Außenbereich, darunter Erde, Gras, Kies, und Sand. Ideal für realistische Landschaftsgestaltung in Ihren Projekten.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 93
//    },

//    new MakerSpaceProject
//    {
//        Id = 94,
//        Title = "Napkin",
//        Tags = "KI, Visualisieren",
//        Top = true,
//        ProjectUrl = "https://app.napkin.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://www.napkin.ai/assets/napkin-logo-2024-beta.svg",
//        Description = "<p>Die künstliche Intelligenz unterstützt die visuelle Ideengestaltung, Ideenorganisation und das Wissensmanagement, indem sie Konzepte vernetzt und Strukturen sichtbar macht. Sie fördert assiziatives Denken, exploratives Lernen und kreative Prozesse wie Design Thinking und Co-Creation- ideal für visuelles Mapping.&nbsp;</p>",
//        DisplayOrder = 94
//    },

//    new MakerSpaceProject
//    {
//        Id = 95,
//        Title = "NotebookLM",
//        Tags = "KI, Schreibprozesse",
//        Top = true,
//        ProjectUrl = "https://notebooklm.google/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/7807fb7a-4031-42ed-bbb5-6abfe8d791d0.svg",
//        Description = "<p>Nutzer können Wissensbasen aus unterschiedlichen Quellen erstellen. KI analysiert die Quellen und beantwortet Fragen. Zudem werden Zusammenfassungen, FAQs und Arbeitshilfen generiert. Die gemeinsame Bearbeitung von Wissensbasen ist möglich.</p>",
//        DisplayOrder = 95
//    },

//    new MakerSpaceProject
//    {
//        Id = 96,
//        Title = "Boden-Texturen (innen)",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/wood/yughues-free-wooden-floor-materials-13213",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/95454c62-09f4-421e-b63e-db7577369d98.webp",
//        Description = "<p>Dieses Asset-Pack bietet hochwertige Boden-Texturen für Innenräume, darunter verschiedene Parkettböden aus unterschiedlichen Holzarten. Perfekt geeignet für realistische Raumgestaltung in Spielen, Architekturvisualisierungen und Simulationen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 96
//    },

//    new MakerSpaceProject
//    {
//        Id = 97,
//        Title = "Otter.ai",
//        Tags = "KI, Transkription",
//        Top = true,
//        ProjectUrl = "https://otter.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://cdn.prod.website-files.com/618e9316785b3582a5178502/65c9f5105c1f5d9effb29333_Otter_Blue_Vertical-p-800.png",
//        Description = "<p>Eine Transkriptionssoftware, die Meetings und Gespräche in Echtzeit aufzeichnen und transkribieren kann.</p>",
//        DisplayOrder = 97
//    },

//    new MakerSpaceProject
//    {
//        Id = 98,
//        Title = "Perplexity AI",
//        Tags = "KI, Literaturrecherche, Textanalyse",
//        Top = true,
//        ProjectUrl = "https://www.perplexity.ai/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1d/Perplexity_AI_logo.svg/768px-Perplexity_AI_logo.svg.png",
//        Description = "<p>KI-Suchmaschine, die in Echtzeit Internetrecherchen durchführt und Antworten mit Quellenangaben liefert. Besonders nützlich für erste Literaturrecherchen.</p>",
//        DisplayOrder = 98
//    },

//    new MakerSpaceProject
//    {
//        Id = 99,
//        Title = "Petalica Paint",
//        Tags = "KI, Bilder",
//        Top = false,
//        ProjectUrl = "https://petalica.com/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/64d33f69-a54e-4069-94a1-6f4f397a633c.svg",
//        Description = "<p>Mit Petalica Paint nutzt eine KI-unterstützte Plattform für die digitale Kunst: Skizzen werden automatisch koloriert, während Künstler aus drei einzigartigen Stilen wählen und ihre Werke mit präzisen Farbhints verfeinern können.</p>",
//        DisplayOrder = 99
//    },

//    new MakerSpaceProject
//    {
//        Id = 100,
//        Title = "Wand-Texturen",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/brick/p3d-outdoor-wall-tile-texture-pack-lr-247739",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/ae547247-7b3d-4eeb-98d0-58c534b94785.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine vielfältige Auswahl an Wand-Texturen, wodurch Sie Ihren Projekten realistische Wände verleihen können.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 100
//    },

//    new MakerSpaceProject
//    {
//        Id = 102,
//        Title = "QuillBot",
//        Tags = "KI, Schreibprozesse, Plagiatprüfung",
//        Top = true,
//        ProjectUrl = "https://quillbot.com/de/",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/ffd74df8-8a5d-40ae-8613-ade516ad21f6.png",
//        Description = "<p>QuillBot verbessert Texte durch Korrekturlesen, Umschreiben, Paraphrasieren und Zusammenfassen. Es spart Zeit und bietet Inspiration für neue Schreibstile.</p>",
//        DisplayOrder = 102
//    },

//    new MakerSpaceProject
//    {
//        Id = 103,
//        Title = "Quizbot",
//        Tags = "KI, Prüfungen, Quiz",
//        Top = false,
//        ProjectUrl = "https://quizbot.ai/de",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = null,
//        Description = "<p>Quizbot ist ein KI-gestützter Fragengenerator, der die Erstellung von Fragen und Prüfungen effizient und präzise optimiert.</p>",
//        DisplayOrder = 103
//    },

//    new MakerSpaceProject
//    {
//        Id = 104,
//        Title = "3D-Büroeinrichtung und Zubehör",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/interior/office-pack-free-258600",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/c3f26526-8631-4d82-a2fa-9cb916ea00f7.webp",
//        Description = "<p>Dieses kostenlose Asset-Pack enthält eine Büroeinrichtung und -zubehör. Enthalten sind unter Anderem:&nbsp;</p><ul><li>Arbeitstische&nbsp;</li><li>Regale und Bücherregale (inklusive Bücher zum einsetzen)&nbsp;</li><li>verschiedene gemütliche Sitzgelegenheiten</li><li>Drucker</li><li>Schreibtischlampe</li><li>Zimmerpflanzen</li><li>Wasserspender</li></ul><p>Quelle: <a href=https://assetstore.unity.com>https://assetstore.unity.com</a></p>",
//        DisplayOrder = 104
//    },

//    new MakerSpaceProject
//    {
//        Id = 105,
//        Title = "3D-Schulmodelle",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/school-assets-146253",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/a2d3d3be-1539-4b59-944e-57d78d1ed352.webp",
//        Description = "<p>Dieses Asset-Pack enthält detaillierte 3D-Modelle rund um das Thema Schule, darunter Klassenzimmer, Schulmöbel, Tafeln, Bücher und vieles mehr. Gestalten Sie Ihre Traumschule wie Sie es möchten.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 105
//    },

//    new MakerSpaceProject
//    {
//        Id = 106,
//        Title = "3D-Sportset",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/characters/free-sports-kit-239377",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/a22ebac6-e282-4b96-915e-5acbac35ff32.webp",
//        Description = "<p>Dieses kostenlose Asset- Pack enthält ein Set aus Sportzubehör für folgende Sportarten:&nbsp;</p><ul><li>Fußball</li><li>Basketball</li><li>Tennis</li><li>Golf</li><li>Volleyball</li></ul><p>Quelle: <a href=https://assetstore.unity.com>https://assetstore.unity.com</a></p>",
//        DisplayOrder = 106
//    },

//    new MakerSpaceProject
//    {
//        Id = 107,
//        Title = "3D-Blumen ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/vegetation/plants/lowpoly-flowers-47083",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/ce36211b-6732-4565-b5df-ec0ca2706c53.webp",
//        Description = "<p>Dieses Asset- Pack enthält ein Set aus verschiedenen Blumen- perfekt für ein Blumenbeet oder eine Landschaft!</p><p>Quelle: <a href=https://assetstore.unity.com>https://assetstore.unity.com</a></p>",
//        DisplayOrder = 107
//    },

//    new MakerSpaceProject
//    {
//        Id = 108,
//        Title = "3D-Szenen",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-atmospheric-locations-pack-278928",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/6418ff06-409e-4702-953d-10ef07657523.webp",
//        Description = "<p>Dieses vielseitige Asset-Pack umfasst 10 detaillierte 3D-Szenen mit unterschiedlichen Themen und Umgebungen - darunter Stadtlandschaften, Dörfer, Häfen, Straßen mit Fahrzeugen, Schiffe und mehr. Die Szenen können auch beliebig umgestaltet werden. Ihrer Kreativität sind keine Grenzen gesetzt.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 108
//    },

//    new MakerSpaceProject
//    {
//        Id = 109,
//        Title = "3D-Marktplatz",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/low-poly-medieval-market-262473",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/964fc471-ec95-4388-a3d9-9917540a40dd.webp",
//        Description = "<p>Dieses Asset-Pack stellt einen mittelalterlichen Marktplatz dar und enthält detailreiche 3D-Modelle von Marktständen, Fisch, Obst, Gemüse, Körben und Tischen. Ideal für historische Spiele, Simulationen und Visualisierungen mit authentischem Mittelalter-Flair.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 109
//    },

//    new MakerSpaceProject
//    {
//        Id = 110,
//        Title = "3D-Kantine (inklusive Essen) ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/food/pandazole-kitchen-food-low-poly-pack-204525",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/ec0f600d-b21e-4f72-9f7b-f3c3b30b52a1.webp",
//        Description = "<p>Dieses kostenlose Asset-Pack enthält eine Kantineneinrichtung inklusive Geschirr, Besteck und einer Vielzahl an unterschiedlichen Speisen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com>https://assetstore.unity.com</a></p>",
//        DisplayOrder = 110
//    },

//    new MakerSpaceProject
//    {
//        Id = 111,
//        Title = "3D-Büro",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/characters/low-poly-office-pack-characters-props-119386",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/3c01bc3b-a0f5-4872-9bc8-e8bc7ff904e1.webp",
//        Description = "<p>Dieses Low-Poly-Asset-Pack zeigt ein stilisiertes Büro mit einer Vielzahl an Geräten und Möbeln, darunter Schreibtische, Stühle, Computer, Drucker, Telefone und mehr. Perfekt geeignet für mobile Spiele, Simulationen oder stilisierte Visualisierungen im Büro-Setting.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 111
//    },

//    new MakerSpaceProject
//    {
//        Id = 112,
//        Title = "3D-Stuhlmodelle",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/low-poly-office-props-lite-131438",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/2f34e519-10ad-4fce-8989-b812e2f0a272.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine Auswahl an 3D-Stühlen in verschiedenen Farben und Designs. Ideal für Innenraumgestaltungen, Visualisierungen und stylisierte Szenen mit variabler Möblierung. Zudem ist es perfekt für die 6-Denkhüte-Methode nach de Bono.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 112
//    },

//    new MakerSpaceProject
//    {
//        Id = 113,
//        Title = "3D-Segelschiff",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/vehicles/sea/stylized-pirate-ship-200192",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/0d476720-573d-4289-890e-d91daf0e089e.webp",
//        Description = "<p>Dieses Asset-Pack enthält ein detailreiches 3D-Segelschiff-Modell mit begehbaren Kabinen und Unterdeck. Perfekt geeignet für Spiele, Simulationen, Animationen oder Visualisierungen mit maritimen Szenen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 113
//    },

//    new MakerSpaceProject
//    {
//        Id = 114,
//        Title = "Awesome-Blender ",
//        Tags = "Blender",
//        Top = false,
//        ProjectUrl = "https://github.com/agmmnn/awesome-blender",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "/images/makerspace/7c473572-2aa1-4d41-be26-cd11fb45a7b3.png",
//        Description = "<p>Das GitHub-Repository <a href=https://github.com/agmmnn/awesome-blender?tab=readme-ov-file#table><strong>agmmnn/awesome-blender</strong></a> bietet eine umfangreiche Sammlung sorgfältig kuratierter Ressourcen für Blender-Nutzer. Diese Sammlung richtet sich an 3D-Künstler, Entwickler, Hobbyisten und Forscher und legt den Fokus auf kostenlose und quelloffene Inhalte.</p>",
//        DisplayOrder = 114
//    },

//    new MakerSpaceProject
//    {
//        Id = 115,
//        Title = "Trinka",
//        Tags = "KI, Schreibassistent",
//        Top = true,
//        ProjectUrl = "https://www.trinka.ai/",
//        Forschung = true,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://www.trinka.ai/assets/images/trinka_logo.svg",
//        Description = "<p>Trinka AI ist ein KI-basierter Schreibassistent, der speziell für akademisches und technisches Englisch entwickelt wurde. Er prüft Grammatik, Stil und Fachsprache auf hohem Niveau und bietet kontextbezogene Verbesserungsvorschläge. Zusätzlich unterstützt Trinka bei der Plagiatsprüfung sowie beim Umschreiben komplexer Sätze. Die Software lässt sich in Tools wie Microsoft Word und gängigen Browsern integrieren und unterstützt auch LaTeX-Dokumente.</p><p>Obwohl Trinka primär auf englischsprachige Texte ausgerichtet ist, kann die Software auch deutsche Texte lesen und verarbeiten - etwa zur Vorabprüfung oder als Teil eines Übersetzungsprozesses.</p>",
//        DisplayOrder = 115
//    },

//    new MakerSpaceProject
//    {
//        Id = 116,
//        Title = "3D-Nadelbäume",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/vegetation/trees/conifers-botd-142076",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/b7488e8a-654e-4b06-937f-09c471292891.webp",
//        Description = "<p>Bring Leben in deine Waldlandschaften mit diesem Nadelbaum-Asset-pack. Die vier unterschiedlichen Varianten ermöglichen es dir, einen natürlich und lebendig wirkenden Nadelwald zu gestalten.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 116
//    },

//    new MakerSpaceProject
//    {
//        Id = 117,
//        Title = "3D-Stadtlandschaft",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/simplepoly-city-low-poly-assets-58899",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/c3b61fc1-009c-43e3-a192-f09a7d30753e.webp",
//        Description = "<p>Erstelle lebendige, detailreiche Stadtlandschaften mit diesem vielseitigen Asset-Pack. Mit einer breiten Auswahl an Gebäuden, Fahrzeugen, Natur-Assets und Stadtelementen bietet es dir alles, was du für die Gestaltung städtischer Szenen brauchst- ganz gleich, ob es sich um einen ruhigen Vorort oder das lebendige Stadtzentrum handelt.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 117
//    },

//    new MakerSpaceProject
//    {
//        Id = 118,
//        Title = "Animierte Tiere (Low-Poly)",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/characters/animals/animals-free-animated-low-poly-3d-models-260727",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/7247dd64-b9e5-43c6-96f5-c34b6c0a49e5.webp",
//        Description = "<p>Hauche deiner Spielumgebung Leben ein mit diesem Asset-Pack, das sieben animierte Tiere enthält. Ob Dschungel, Bauernhof oder Wohnzimmer- diese animierten Tiere fügen sich nahtlos in verschiedenste Low-Poly Spielwelten ein.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 118
//    },

//    new MakerSpaceProject
//    {
//        Id = 119,
//        Title = "Wikingerdorf",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/essentials/tutorial-projects/viking-village-urp-29140",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/a6587a98-62df-4b0f-a441-ce33836990e8.webp",
//        Description = "<p>Erschaffe eine atmosphärische Wikingerwelt mit diesem Umgebungspaket. Ob am Fjord, Seeufer oder in rauer Wildnis- mit detailreichen Hütten, Zäunen, Fässern, Fackeln, Vegetation und vielem mehr lässt sich ein authentisches und lebendiges Dorf voller Geschichte gestalten.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 119
//    },

//    new MakerSpaceProject
//    {
//        Id = 120,
//        Title = "Moderne Stadtlandschaft",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/urban/city-package-107224",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/bc411d15-8ece-4ecb-8cef-edaf336f5e84.webp",
//        Description = "<p>Baue eine moderne Stadt mit diesem umfangreichen Asset-Pack, das eine große Auswahl an Gebäuden, Straßen und städtischer Vegetation bietet. Von Polizei, Feuerwehr und Krankenhaus bis hin zu Wohnhäusern, Läden und einer Bank- dieses Asset-Pack bietet 45 verschiedene Gebäude. Ergänzt durch 189 Props wie Straßenschilder, Mülltonnen, Parkbänke, Zäune und mehr, kannst du damit deine Stadt nach deinen individuellen Vorstellungen modellieren.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 120
//    },

//    new MakerSpaceProject
//    {
//        Id = 121,
//        Title = "3D-Blumenwiese",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/nature/grass-flowers-pack-free-138810",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/7df2a515-3194-4fc8-8df1-db459fe9e01b.webp",
//        Description = "<p>Dieses Asset-Pack enthält 12 detailreiche und realistische Gras- und Blumen-Texturen mit denen farbenfrohe Blumenwiesen gestaltet werden können.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 121
//    },

//    new MakerSpaceProject
//    {
//        Id = 122,
//        Title = "Geflutete Stadt",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/flooded-grounds-48529",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/10ff3322-8a41-4636-bade-bf7fff81406d.webp",
//        Description = "<p>Tauche ein in eine postapokalyptische Atmosphäre mit diesem Asset-Pack einer gefluteten Stadt. Enthalten sind Industriegebäude, Wohnhäuser, Kirchen und mehr- alle vom Wasser beschädigt und verlassen. Ideal als Vorlage für inspirierende Storys, Survival-Szenarien oder stimmungsvolle Umgebungen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 122
//    },

//    new MakerSpaceProject
//    {
//        Id = 123,
//        Title = "Naturumgebung (Low-Poly)",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/low-poly-environment-315184",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/e70bead0-b0a8-4684-90ad-4b8a2aeb852a.webp",
//        Description = "<p>Erstelle Low-Poly Naturlandschaften mit diesem modularen Umgebungspaket. Es bietet eine Auswahl an Bergen, Klippen, Wasserfällen, Häusern, Straßen und Fahrzeugen.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 123
//    },

//    new MakerSpaceProject
//    {
//        Id = 124,
//        Title = "3D-Holztüren ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/interior/free-wood-door-pack-280509",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/d0430e4b-b630-4e4d-942e-658a635097ae.webp",
//        Description = "<p>Dieses Asset-Pack enthält 15 Holztür-Modelle, die sich vollständig öffnen und animieren lassen. Mit 105 farblichen Prefab-Varianten bietet es maximale Vielfalt für stilistisch unterschiedliche Szenen.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 124
//    },

//    new MakerSpaceProject
//    {
//        Id = 125,
//        Title = "Waldpaket- 51 realistische Baummodelle",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/vegetation/trees/european-forests-realistic-trees-229716",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/c363896c-5ce6-486e-bd7c-a48b0c0be35c.webp",
//        Description = "<p>Dieses Asset-Pack enthält 51 realistische abwechslungsreiche Baum-Modelle zur Gestaltung von Naturumgebungen. Ideal zur Gestaltung von Wäldern, Parks oder offenen Landschaften.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 125
//    },

//    new MakerSpaceProject
//    {
//        Id = 126,
//        Title = "MicroSplat",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/tools/terrain/microsplat-96478",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/1ee333b4-4d58-4581-9693-6be5ea21e9e8.webp",
//        Description = "<p>Microsplat ist ein Terrain-Shading-System für Unity, das realistische und hochperformative Bodendarstellungen ermöglicht. Mit erweiterten Funktionen wie dynamischer Nässe, Schnee, Triplanar-Mapping und optimiertem Texturhandling hebt es Terrain-Gestaltung auf ein neues Niveau.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 126
//    },

//    new MakerSpaceProject
//    {
//        Id = 127,
//        Title = "Café ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/coffee-shop-environment-217600",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/bb868e29-1d38-4a11-b03e-c58a79bcebda.webp",
//        Description = "<p>Dieses Asset-Pack bietet eine atmosphärische Café-Umgebung mit Möbeln, Theken, Geräten und sorgfältig gestalteten Details.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 127
//    },

//    new MakerSpaceProject
//    {
//        Id = 128,
//        Title = "Low-Poly Einrichtung",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/furniture/furniture-free-low-poly-3d-models-pack-260522",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/f5461b83-77fa-4c4f-9100-fa9d6ad57bff.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine breite Auswahl an Möbeln. Enthalten sind unter Anderem:&nbsp;</p><ul><li>Stühle</li><li>Sofa</li><li>Bett</li><li>Kommoden</li><li>Schränke&nbsp;</li><li>Tische</li><li>Klavier</li><li>Hantelbank</li></ul><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 128
//    },

//    new MakerSpaceProject
//    {
//        Id = 129,
//        Title = "Verlassenes Fabrikgelände ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/industrial/abandoned-factory-lite-62597",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/2b9bd254-ca97-4f8d-8199-1b0aabb3b98e.webp",
//        Description = "<p>Dieses Asset-Pack enthält realistisch gestaltete Gebäudeteile, Rohre und Wracks, die eine authentische verlassene Fabrik-Atmosphäre schaffen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 129
//    },

//    new MakerSpaceProject
//    {
//        Id = 130,
//        Title = "Umfangreiches Low-Poly Umgebungspaket",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/pandazole-lowpoly-asset-bundle-226938",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/dc7e97fd-3ebb-4ff8-bf23-2f2293bca7c4.webp",
//        Description = "<p>Dieses riesige Asset-Pack enthält mehr als 1650 Low-Poly-Modelle. Von Pflanzen, Möbeln und Gebäuden bis hin zu Straßenschildern, Böden, Flüssen, Wegen und einer großen Auswahl an Speisen- ist alles enthalten, um eine Low-Poly Umgebung nach eigenen Vorstellungen zu schaffen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 130
//    },

//    new MakerSpaceProject
//    {
//        Id = 131,
//        Title = "Möblierte Hütte",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/urban/furnished-cabin-71426",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/package-screenshot/446ae928-a3b6-4886-ad65-7b8f3e967b47.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine vollständig möblierte Hütte inklusive Dekoration und strukturellem Aufbau. Perfekt geeignet für Umgebungen mit gemütlicher Atmosphäre.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 131
//    },

//    new MakerSpaceProject
//    {
//        Id = 132,
//        Title = "Foliage Engine Light- effiziente Vegetationslösung für Unity ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/vfx/shaders/the-toby-foliage-engine-light-282901",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/2c302d82-8a01-4179-b98d-05d69f885feb.webp",
//        Description = "<p>Diese Foliage Engine ermöglicht die performative Darstellung (inklusive Windanimation) von Gräsern, Büschen, Bäumen und anderen Pflanzen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 132
//    },

//    new MakerSpaceProject
//    {
//        Id = 133,
//        Title = "Modulares Science-Fiction-Pack",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/3d-scifi-kit-starter-kit-92152",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/6bad829f-06a4-4bbd-aa4b-591ef2c3f6fc.webp",
//        Description = "<p>Dieses Asset-Pack enthält Wände, Böden, Türen, Requisiten und mehr zur Gestaltung von futuristischen Umgebungen. Es eignet sich optimal zur Erstellung von Raumschiff-Umgebungen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 133
//    },

//    new MakerSpaceProject
//    {
//        Id = 134,
//        Title = "Low-Poly Farm",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/industrial/low-poly-farm-pack-lite-188100",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/0f20fb06-37b0-4e5e-80d4-0d99c189edc7.webp",
//        Description = "<p>Dieses Asset-Pack enthält Bäume, Beete, Tomaten- und Salat-Pflanzen und Körbe, die mit der Ernte gefüllt sind. Die perfekte Wahl für die Erstellung von Low-Poly Farmumgebungen oder ländlichen Szenen.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 134
//    },

//    new MakerSpaceProject
//    {
//        Id = 135,
//        Title = "Fast-Food Restaurant",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/fast-food-restaurant-kit-239419",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/14253f1a-ceea-42ca-99f2-5806003a400c.webp",
//        Description = "<p>Dieses Asset-Pack bietet alles, was du für die Erstellung eines typischen Fast-Food-Restaurants brauchst. Enthalten sind unter Anderem ein typisches Straßenschild, Sitzgelegenheiten, Getränkeautomaten, Kassen und digitale Bestellterminale.</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 135
//    },

//    new MakerSpaceProject
//    {
//        Id = 136,
//        Title = "3D-Waldvegetation",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/3d-nature-assetspack-215646",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/853fc874-adeb-4294-9e1d-7b8b783b63e0.webp",
//        Description = "<p>Dieses Asset-Pack bietet eine Auswahl an Bäumen, Pilzen, Gräsern und Blumen zur Ergänzung bestehender Szenen oder zum Erstellen eigener Waldumgebungen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 136
//    },

//    new MakerSpaceProject
//    {
//        Id = 137,
//        Title = "3D-Küchenset",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/interior/free-kitchen-cabinets-and-equipment-245554",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/faecb0ac-8265-4961-bcf9-ccb643ad302f.webp",
//        Description = "<p>Mit drei verschiedenen Küchenvarianten- blau, rosa und grün- sowie 40 verschiedenen Modellen bietet das Asset-Pack vielfältige Möglichkeiten zur Gestaltung von Küchenumgebungen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 137
//    },

//    new MakerSpaceProject
//    {
//        Id = 138,
//        Title = "3D-Spielplatz ",
//        Tags = "Unity ",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/playground-low-poly-191533",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/49d00609-3ed1-4e7c-87c9-8cde8ee26f51.webp",
//        Description = "<p>Mit einer großen Auswahl an Spielgeräten wie Klettergerüsten, Rutschen, Schaukeln und mehr bietet dieses Asset-Pack alles, was für einen spannend gestalteten Spielplatz nötig ist. Optimal geeignet zur Gestaltung von Parks, Schulhöfen und familienfreundlichen Umgebungen.&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 138
//    },

//    new MakerSpaceProject
//    {
//        Id = 139,
//        Title = "3D-Lebensmittel",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/props/food/food-pack-free-demo-225294",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/12ef1741-7aee-488c-8f06-1e0e6c3bc67d.webp",
//        Description = "<p>Dieses Asset-Pack enthält eine vielfältige Sammlung von 3D-Lebensmittelmodellen&nbsp;- von frischem Obst und Gemüse bis hin zu Pizza und Fleisch. Ideal für den Einsatz in Spielen, Simulationen, Visualisierungen oder virtuellen Marktszenen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 139
//    },

//    new MakerSpaceProject
//    {
//        Id = 140,
//        Title = "Boden-Texturen (universal)",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/2d/textures-materials/25-free-realistic-textures-nature-city-home-construction-more-240323",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/7e018605-383e-416e-ba92-29599a2cc8f4.webp",
//        Description = "<p>Dieses Asset-Pack bietet eine Auswahl an hochwertigen Boden-Texturen für Innen- und Außenbereiche - von edlem Parkett und Fliesen bis hin zu Naturstein, Asphalt und Erde. Perfekt geeignet für realistische Visualisierungen in Ihrem Projekt.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 140
//    },

//    new MakerSpaceProject
//    {
//        Id = 141,
//        Title = "Realistische Inneneinrichtung",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/urban/devden-archviz-vol-1-scotland-urp-204933",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/6d55a8a0-16a8-4bf4-a5df-90a1846b41c1.webp",
//        Description = "<p>Dieses Asset-Pack enthält realistische 3D-Modelle einer vollständigen Inneneinrichtung - von Möbeln und Dekoration bis hin zu alltäglichen Haushaltsgegenständen. Ideal für Architekturvisualisierungen, Interior-Design-Konzepte und virtuelle Umgebungen</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 141
//    },

//    new MakerSpaceProject
//    {
//        Id = 142,
//        Title = "SeedMesh Shader für Pflanzen",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/vfx/shaders/seedmesh-vegetation-shaders-232690",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/8467f739-5d81-4837-8d3a-9a0a8e042c7e.webp",
//        Description = "<p>Verleihen Sie Ihren Projekten mehr Leben mit SeedMesh Shader! Es enthält einen anpassbaren Shader, der realistische Bewegungen für Vegetation und Pflanzen erzeugt - vom sanften Rascheln der Blätter bis hin zum rhythmischen Wiegen ganzer Sträucher im Wind. Ideal für stylisierte oder realistische Umgebungen in Unity.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 142
//    },

//    new MakerSpaceProject
//    {
//        Id = 143,
//        Title = "3D-Birken",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/vegetation/trees/urp-white-birch-tree-mobile-281448",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/2069e1ba-e668-4b54-b19d-c53cfab558fe.webp",
//        Description = "<p>Bringen Sie Vielfalt und Realismus in Ihre Umgebungen mit diesem hochwertigen Assetpack! Enthalten sind 17 verschiedene 3D-Modelle von Birken, die stark für eine herausragende Performance optimiert sind.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 143
//    },

//    new MakerSpaceProject
//    {
//        Id = 144,
//        Title = "Mittelalterliches Lernzimmer",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/3d/environments/free-medieval-room-131004",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/ab56a4ba-ebc6-48ea-87ab-8eb8a19ea183.webp",
//        Description = "<p>Tauchen Sie ein in die Welt vergangener Zeiten mit diesem detailreichen 3D-Assetpack eines mittelalterlichen Lernzimmers. Es enthält liebevoll gestaltete Möbel, Bücher, Wanddekorationen und weiteres Zubehör, um eine authentische und stimmungsvolle Lernumgebung zu erschaffen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 144
//    },

//    new MakerSpaceProject
//    {
//        Id = 145,
//        Title = "Procedual World Generation",
//        Tags = "Unity",
//        Top = false,
//        ProjectUrl = "https://assetstore.unity.com/packages/tools/terrain/mega-world-free-207301",
//        Forschung = false,
//        download = false,
//        tutorial = false,
//        events = false,
//        netzwerk = false,

//        ImageUrl = "https://assetstorev1-prd-cdn.unity3d.com/key-image/80ed08cd-d9dc-4dec-8600-317716482eff.webp",
//        Description = "<p>Erweitern Sie Ihre Projekte mit diesem vielseitigen Assetpack, welches Ihnen ermöglicht, Ihre Szenen mit prozedural erzeugten Objekten wie Bäumen, Steinen und weiteren Naturdetails zu bereichern. Durch die automatische Variation entstehen natürliche und abwechslungsreiche Umgebungen, die Ihre Welten lebendiger und realistischer wirken lassen.</p><p>&nbsp;</p><p>Quelle: <a href=https://assetstore.unity.com/>https://assetstore.unity.com/</a></p>",
//        DisplayOrder = 145
//    }
//);
        }
    }
}
