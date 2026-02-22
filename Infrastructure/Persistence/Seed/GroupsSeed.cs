// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;

namespace Klacks.Api.Data.Seed
{
    public static class GroupsSeed
    {
        public static Dictionary<string, Guid> CantonGroupIds { get; private set; } = new Dictionary<string, Guid>();

        public static Dictionary<string, Guid> CityGroupIds { get; private set; } = new Dictionary<string, Guid>();
        
        public static string GenerateInsertScriptForGroups()
        {
            CantonGroupIds.Clear(); // Clear for fresh generation
            CityGroupIds.Clear(); // Clear for fresh generation
            StringBuilder script = new StringBuilder();
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var validFrom = "2025-01-01 00:00:00+01";
            var adminUser = "672f77e8-e479-4422-8781-84d218377fb3";

            // Root 1: Westschweiz (GE, VD, NE, JU, FR) - parent=NULL, root=NULL
            var westschweizId = Guid.NewGuid();
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{westschweizId}', '', 'Westschweiz', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 1, NULL, 22, NULL);");

            // GE - Genf
            var geId = Guid.NewGuid();
            CantonGroupIds["GE"] = geId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{geId}', 'Genf', 'GE', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 2, '{westschweizId}', 5, '{westschweizId}');");
            var genfStadtId = Guid.NewGuid();
            CityGroupIds["Genf Stadt"] = genfStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{genfStadtId}', 'Genf (ca. 203.000)', 'Genf Stadt', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 3, '{geId}', 4, '{westschweizId}');");

            // VD - Waadt  
            var vdId = Guid.NewGuid();
            CantonGroupIds["VD"] = vdId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{vdId}', 'Waadt', 'VD', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 6, '{westschweizId}', 9, '{westschweizId}');");
            var lausanneId = Guid.NewGuid();
            CityGroupIds["Lausanne"] = lausanneId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{lausanneId}', 'Lausanne (ca. 140.000)', 'Lausanne', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 7, '{vdId}', 8, '{westschweizId}');");

            // NE - Neuenburg
            var neId = Guid.NewGuid();
            CantonGroupIds["NE"] = neId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{neId}', 'Neuenburg', 'NE', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 10, '{westschweizId}', 13, '{westschweizId}');");
            var neuchatelId = Guid.NewGuid();
            CityGroupIds["Neuchâtel"] = neuchatelId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{neuchatelId}', 'Neuchâtel (Hauptstadt)', 'Neuchâtel', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 11, '{neId}', 12, '{westschweizId}');");

            // JU - Jura (in Westschweiz)
            var juId = Guid.NewGuid();
            CantonGroupIds["JU"] = juId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{juId}', 'Jura', 'JU', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 14, '{westschweizId}', 17, '{westschweizId}');");
            var delemontId = Guid.NewGuid();
            CityGroupIds["Delémont"] = delemontId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{delemontId}', 'Delémont (Hauptstadt)', 'Delémont', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 15, '{juId}', 16, '{westschweizId}');");

            // FR - Freiburg (in Westschweiz)
            var frId = Guid.NewGuid();
            CantonGroupIds["FR"] = frId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{frId}', 'Freiburg', 'FR', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 18, '{westschweizId}', 21, '{westschweizId}');");
            var fribourgId = Guid.NewGuid();
            CityGroupIds["Fribourg"] = fribourgId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{fribourgId}', 'Fribourg (Hauptstadt)', 'Fribourg', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 19, '{frId}', 20, '{westschweizId}');");

            // Root 2: Deutschweiz Zürich (ZH, AG) - parent=NULL, root=NULL
            var deutschweizZurichId = Guid.NewGuid();
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{deutschweizZurichId}', '', 'Deutschweiz Zürich', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 1, NULL, 12, NULL);");

            // ZH - Zürich
            var zhId = Guid.NewGuid();
            CantonGroupIds["ZH"] = zhId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{zhId}', 'Zürich', 'ZH', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 2, '{deutschweizZurichId}', 9, '{deutschweizZurichId}');");
            var zurichStadtId = Guid.NewGuid();
            CityGroupIds["Zürich"] = zurichStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{zurichStadtId}', 'Zürich (ca. 434.000)', 'Zürich', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 3, '{zhId}', 4, '{deutschweizZurichId}');");
            var winterthurId = Guid.NewGuid();
            CityGroupIds["Winterthur"] = winterthurId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{winterthurId}', 'Winterthur (ca. 115.000)', 'Winterthur', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 5, '{zhId}', 6, '{deutschweizZurichId}');");
            var usterId = Guid.NewGuid();
            CityGroupIds["Uster"] = usterId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{usterId}', 'Uster (ca. 37.000)', 'Uster', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 7, '{zhId}', 8, '{deutschweizZurichId}');");

            // AG - Aargau
            var agId = Guid.NewGuid();
            CantonGroupIds["AG"] = agId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{agId}', 'Aargau', 'AG', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 10, '{deutschweizZurichId}', 11, '{deutschweizZurichId}');");

            // Root 3: Deutschweiz Mitte (BE, SO, BS, BL) - parent=NULL, root=NULL
            var deutschweizMitteId = Guid.NewGuid();
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{deutschweizMitteId}', '', 'Deutschweiz Mitte', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 1, NULL, 30, NULL);");

            // BE - Bern
            var beId = Guid.NewGuid();
            CantonGroupIds["BE"] = beId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{beId}', 'Bern', 'BE', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 2, '{deutschweizMitteId}', 11, '{deutschweizMitteId}');");
            var bernStadtId = Guid.NewGuid();
            CityGroupIds["Bern"] = bernStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{bernStadtId}', 'Bern (ca. 134.000)', 'Bern', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 3, '{beId}', 4, '{deutschweizMitteId}');");
            var bielBienneId = Guid.NewGuid();
            CityGroupIds["Biel/Bienne"] = bielBienneId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{bielBienneId}', 'Biel/Bienne (ca. 55.000)', 'Biel/Bienne', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 5, '{beId}', 6, '{deutschweizMitteId}');");
            var thunId = Guid.NewGuid();
            CityGroupIds["Thun"] = thunId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{thunId}', 'Thun (ca. 44.000)', 'Thun', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 7, '{beId}', 8, '{deutschweizMitteId}');");
            var koenizId = Guid.NewGuid();
            CityGroupIds["Köniz"] = koenizId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{koenizId}', 'Köniz (ca. 42.000)', 'Köniz', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 9, '{beId}', 10, '{deutschweizMitteId}');");

            // SO - Solothurn
            var soId = Guid.NewGuid();
            CantonGroupIds["SO"] = soId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{soId}', 'Solothurn', 'SO', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 12, '{deutschweizMitteId}', 15, '{deutschweizMitteId}');");
            var solothurnStadtId = Guid.NewGuid();
            CityGroupIds["Solothurn Stadt"] = solothurnStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{solothurnStadtId}', 'Solothurn (Hauptstadt)', 'Solothurn Stadt', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 13, '{soId}', 14, '{deutschweizMitteId}');");

            // BS - Basel Stadt
            var bsId = Guid.NewGuid();
            CantonGroupIds["BS"] = bsId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{bsId}', 'Basel Stadt', 'BS', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 16, '{deutschweizMitteId}', 19, '{deutschweizMitteId}');");
            var baselStadtId = Guid.NewGuid();
            CityGroupIds["Basel"] = baselStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{baselStadtId}', 'Basel (ca. 178.000)', 'Basel', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 17, '{bsId}', 18, '{deutschweizMitteId}');");

            // BL - Basel Land
            var blId = Guid.NewGuid();
            CantonGroupIds["BL"] = blId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{blId}', 'Basel Land', 'BL', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 20, '{deutschweizMitteId}', 29, '{deutschweizMitteId}');");
            var allschwilId = Guid.NewGuid();
            CityGroupIds["Allschwil"] = allschwilId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{allschwilId}', 'Allschwil (ca. 20.000)', 'Allschwil', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 21, '{blId}', 22, '{deutschweizMitteId}');");
            var reinachBLId = Guid.NewGuid();
            CityGroupIds["Reinach BL"] = reinachBLId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{reinachBLId}', 'Reinach (ca. 19.000)', 'Reinach BL', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 23, '{blId}', 24, '{deutschweizMitteId}');");
            var muttenzId = Guid.NewGuid();
            CityGroupIds["Muttenz"] = muttenzId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{muttenzId}', 'Muttenz (ca. 17.000)', 'Muttenz', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 25, '{blId}', 26, '{deutschweizMitteId}');");
            var liestalId = Guid.NewGuid();
            CityGroupIds["Liestal"] = liestalId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{liestalId}', 'Liestal (Hauptstadt)', 'Liestal', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 27, '{blId}', 28, '{deutschweizMitteId}');");

            // Root 4: Deutschweiz Ost (Alle anderen Kantone ohne FR) - parent=NULL, root=NULL
            var deutschweizOstId = Guid.NewGuid();
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES
            ('{deutschweizOstId}', '', 'Deutschweiz Ost', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, 1, NULL, 52, NULL);");

            var currentLft = 2;

            // LU - Luzern (2-6)
            var luId = Guid.NewGuid();
            CantonGroupIds["LU"] = luId;
            var luRgt = currentLft + 4;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{luId}', 'Luzern', 'LU', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{deutschweizOstId}', {luRgt}, '{deutschweizOstId}');");
            var luzernStadtId = Guid.NewGuid();
            CityGroupIds["Luzern Stadt"] = luzernStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{luzernStadtId}', 'Luzern (ca. 82.000)', 'Luzern Stadt', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{luId}', {currentLft++}, '{deutschweizOstId}');");
            var emmenId = Guid.NewGuid();
            CityGroupIds["Emmen"] = emmenId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{emmenId}', 'Emmen (ca. 30.000)', 'Emmen', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{luId}', {currentLft++}, '{deutschweizOstId}');");

            // SG - St. Gallen (7-12)
            var sgId = Guid.NewGuid();
            CantonGroupIds["SG"] = sgId;
            var sgRgt = currentLft + 5;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{sgId}', 'St. Gallen', 'SG', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{deutschweizOstId}', {sgRgt}, '{deutschweizOstId}');");
            var stGallenStadtId = Guid.NewGuid();
            CityGroupIds["St. Gallen Stadt"] = stGallenStadtId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{stGallenStadtId}', 'St. Gallen (ca. 76.000)', 'St. Gallen Stadt', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{sgId}', {currentLft++}, '{deutschweizOstId}');");
            var rapperswilJonaId = Guid.NewGuid();
            CityGroupIds["Rapperswil-Jona"] = rapperswilJonaId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{rapperswilJonaId}', 'Rapperswil-Jona (ca. 27.000)', 'Rapperswil-Jona', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{sgId}', {currentLft++}, '{deutschweizOstId}');");

            // TG - Thurgau (13-15)
            var tgId = Guid.NewGuid();
            CantonGroupIds["TG"] = tgId;
            var tgRgt = currentLft + 2;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{tgId}', 'Thurgau', 'TG', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{deutschweizOstId}', {tgRgt}, '{deutschweizOstId}');");
            var frauenfeldId = Guid.NewGuid();
            CityGroupIds["Frauenfeld"] = frauenfeldId;
            script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
            ('{frauenfeldId}', 'Frauenfeld (Hauptstadt)', 'Frauenfeld', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{tgId}', {currentLft++}, '{deutschweizOstId}');");

            // Alle restlichen Kantone mit Hauptstädten (FR wurde nach Westschweiz verschoben)
            var remainingCantonsWithCapitals = new[]
            {
                ("AI", "Appenzell Innerrhoden", "Appenzell"),
                ("AR", "Appenzell Ausserrhoden", "Herisau"), 
                ("GL", "Glarus", "Glarus"),
                ("GR", "Graubünden", "Chur"),
                ("NW", "Nidwalden", "Stans"),
                ("OW", "Obwalden", "Sarnen"),
                ("SH", "Schaffhausen", "Schaffhausen"),
                ("SZ", "Schwyz", "Schwyz"),
                ("TI", "Tessin", "Bellinzona"),
                ("UR", "Uri", "Altdorf"),
                ("VS", "Wallis", "Sion"),
                ("ZG", "Zug", "Zug")
            };

            foreach (var (code, name, capital) in remainingCantonsWithCapitals)
            {
                var cantonId = Guid.NewGuid();
                CantonGroupIds[code] = cantonId;
                var cantonRgt = currentLft + 2;
                script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
                ('{cantonId}', '{name}', '{code}', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{deutschweizOstId}', {cantonRgt}, '{deutschweizOstId}');");
                var capitalId = Guid.NewGuid();
                CityGroupIds[capital] = capitalId;
                script.AppendLine($@"INSERT INTO public.""group"" (id, description, name, valid_from, valid_until, create_time, current_user_created, current_user_deleted, current_user_updated, deleted_time, is_deleted, update_time, lft, parent, rgt, root) VALUES 
                ('{capitalId}', '{capital} (Hauptstadt)', '{capital}', '{validFrom}', NULL, '{now}', '{adminUser}', NULL, NULL, NULL, false, NULL, {currentLft++}, '{cantonId}', {currentLft++}, '{deutschweizOstId}');");
            }

            return script.ToString();
        }
    }
}