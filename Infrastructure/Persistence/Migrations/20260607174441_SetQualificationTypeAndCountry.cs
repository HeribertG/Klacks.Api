// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets the correct type (1=Language, 2=Work) and country ISO codes
/// for all existing qualification seed rows introduced in previous migrations.
/// </summary>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetQualificationTypeAndCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Languages: type=1 with per-row country codes
UPDATE qualification SET type = 1, country = 'DE' WHERE id = '744d6e64-3ddf-41d0-87c8-30b690c451d9';
UPDATE qualification SET type = 1, country = 'GB' WHERE id = '17e5181d-8b20-4ee5-8e15-5beca491f2c0';
UPDATE qualification SET type = 1, country = 'FR' WHERE id = '664351fc-0c5a-4610-9c05-f5faf778ff90';
UPDATE qualification SET type = 1, country = 'IT' WHERE id = '4a272d5a-d188-4b1e-9631-5870171c8d8f';
UPDATE qualification SET type = 1, country = 'JP' WHERE id = '3f39546f-5573-44a9-8260-4bd28574eadb';
UPDATE qualification SET type = 1, country = 'SA' WHERE id = 'e037374a-1a39-4020-8a13-ee1051dd359f';
UPDATE qualification SET type = 1, country = 'IL' WHERE id = 'cef4f851-2ce1-4c86-8367-5fb890c98334';
UPDATE qualification SET type = 1, country = 'CN' WHERE id = '178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4';
UPDATE qualification SET type = 1, country = 'KR' WHERE id = '0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e';

-- Spitex CH: type=2 (default), country='CH'
UPDATE qualification SET country = 'CH' WHERE id IN (
    '53d33bd0-cd52-48e9-ac0e-618bc94760c7',
    '72a86d8b-fc16-49da-b5d9-e6feee81c329',
    'eb12579a-9262-44a6-be60-d3c9be2954f6',
    'd75c138d-23f1-4064-a3b7-5c202d9e9363',
    'cd74f2e9-add7-42f5-b707-3caf1597a25e',
    'a8bb8129-a47b-4b7b-be2c-4c3b918b19ea',
    'ac78925e-5019-40be-8db6-d0d20f444171',
    '75b24b9c-4a48-4818-be68-4f65744463d8',
    'd98105e2-b60c-4d37-9659-8d685c514ebc',
    '18c4ae01-7e43-46b4-a4e4-bd12437bd5e3',
    'b702a794-c83c-4d94-a3fa-f28b850b79e6',
    '3fd89ae5-21e6-474a-87de-07fdbbe82c41',
    '1190a2fd-c313-457f-b186-3b189ca59344',
    '239c2e84-49d6-4023-b982-3658cfc85b4c'
);

-- Spitex International: type=2, individual countries
UPDATE qualification SET country = 'EU'   WHERE id = 'dc70fef8-2822-430e-9d25-e6a709984d7e';
UPDATE qualification SET country = 'JP'   WHERE id = '4df5b030-7650-4f6d-93cf-0d5e29658bac';
UPDATE qualification SET country = 'SA'   WHERE id = '5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1';
UPDATE qualification SET country = NULL   WHERE id = '68d28df9-113a-40cf-bf6b-3c9bb18660e3';
UPDATE qualification SET country = 'IL'   WHERE id = 'b9154bfa-5abb-4cd7-9f3b-16a339996add';
UPDATE qualification SET country = 'CN'   WHERE id = 'e3201f77-d5f2-4a8b-af49-8214b3eeda95';
UPDATE qualification SET country = 'TW'   WHERE id = 'c5ecea26-4bd7-4080-a728-aa26ebd7b226';
UPDATE qualification SET country = 'KR'   WHERE id = '40b1bcf6-bb79-40aa-9f59-f60aa381021a';

-- Security CH: type=2 (default), country='CH' for certified rows
UPDATE qualification SET country = 'CH' WHERE id IN (
    'ea7b299d-96fd-47c2-83f3-e5ba340e4b73',
    'cb3b1969-9c8b-4b51-b9d0-a534d3073f87'
);

-- Security International: type=2, individual countries
UPDATE qualification SET country = 'DE' WHERE id = 'b257acc2-67ea-4b81-9038-f774c863962e';
UPDATE qualification SET country = 'JP' WHERE id = 'e8354115-4462-4c0d-8fce-a35369e967cb';
UPDATE qualification SET country = 'SA' WHERE id = '8ddfb12f-0373-4c24-ab56-1390d2895e39';
UPDATE qualification SET country = 'IL' WHERE id = 'f7521782-553d-4cf2-9e81-7ec88c51af60';
UPDATE qualification SET country = 'CN' WHERE id = '0127ea12-fa4b-4a11-8dd7-b78d93e65779';
UPDATE qualification SET country = 'KR' WHERE id = 'bd8f022d-0f17-4c11-bc97-151015b3b5d2';
UPDATE qualification SET country = 'TW' WHERE id = '917f486e-e48a-4e11-bd68-a531702b20a8';

-- Logistics CH: type=2 (default), country='CH' for Swiss-specific licences
UPDATE qualification SET country = 'CH' WHERE id IN (
    '9a185d56-180a-4450-8aee-ea98ac897f49',
    '88800ec7-b4fb-471c-ac30-2b759743cf67',
    '6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd',
    '19f57576-eb75-417f-986d-a33cc83b6502',
    '90d38d22-a05e-46cb-a4e2-deea6d3cc9f4'
);

-- Logistics International: type=2, individual countries
UPDATE qualification SET country = 'EU' WHERE id = '9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c';
UPDATE qualification SET country = 'JP' WHERE id = '17615f16-9d2b-42e7-9e61-b1f0a407bb3b';
UPDATE qualification SET country = 'IL' WHERE id = '0571b080-ec96-41d7-9852-e49ac432ab83';
UPDATE qualification SET country = 'CN' WHERE id = 'abaef6e9-0172-4acd-9204-ff5980e976c2';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE qualification SET type = 2, country = NULL WHERE id IN (
    -- Languages
    '744d6e64-3ddf-41d0-87c8-30b690c451d9',
    '17e5181d-8b20-4ee5-8e15-5beca491f2c0',
    '664351fc-0c5a-4610-9c05-f5faf778ff90',
    '4a272d5a-d188-4b1e-9631-5870171c8d8f',
    '3f39546f-5573-44a9-8260-4bd28574eadb',
    'e037374a-1a39-4020-8a13-ee1051dd359f',
    'cef4f851-2ce1-4c86-8367-5fb890c98334',
    '178a94e7-cca2-473c-bbd4-9dcfcd6e6ed4',
    '0ac52ff7-6a4d-4c5d-a59a-4b550b79a47e',
    -- Spitex CH
    '53d33bd0-cd52-48e9-ac0e-618bc94760c7',
    '72a86d8b-fc16-49da-b5d9-e6feee81c329',
    'eb12579a-9262-44a6-be60-d3c9be2954f6',
    'd75c138d-23f1-4064-a3b7-5c202d9e9363',
    'cd74f2e9-add7-42f5-b707-3caf1597a25e',
    'a8bb8129-a47b-4b7b-be2c-4c3b918b19ea',
    'ac78925e-5019-40be-8db6-d0d20f444171',
    '75b24b9c-4a48-4818-be68-4f65744463d8',
    'd98105e2-b60c-4d37-9659-8d685c514ebc',
    '18c4ae01-7e43-46b4-a4e4-bd12437bd5e3',
    'b702a794-c83c-4d94-a3fa-f28b850b79e6',
    '3fd89ae5-21e6-474a-87de-07fdbbe82c41',
    '1190a2fd-c313-457f-b186-3b189ca59344',
    '239c2e84-49d6-4023-b982-3658cfc85b4c',
    -- Spitex International
    'dc70fef8-2822-430e-9d25-e6a709984d7e',
    '4df5b030-7650-4f6d-93cf-0d5e29658bac',
    '5d18ad1d-f1f3-4dfc-9c0e-0836ba87c7e1',
    '68d28df9-113a-40cf-bf6b-3c9bb18660e3',
    'b9154bfa-5abb-4cd7-9f3b-16a339996add',
    'e3201f77-d5f2-4a8b-af49-8214b3eeda95',
    'c5ecea26-4bd7-4080-a728-aa26ebd7b226',
    '40b1bcf6-bb79-40aa-9f59-f60aa381021a',
    -- Security CH
    'ea7b299d-96fd-47c2-83f3-e5ba340e4b73',
    'cb3b1969-9c8b-4b51-b9d0-a534d3073f87',
    -- Security International
    'b257acc2-67ea-4b81-9038-f774c863962e',
    'e8354115-4462-4c0d-8fce-a35369e967cb',
    '8ddfb12f-0373-4c24-ab56-1390d2895e39',
    'f7521782-553d-4cf2-9e81-7ec88c51af60',
    '0127ea12-fa4b-4a11-8dd7-b78d93e65779',
    'bd8f022d-0f17-4c11-bc97-151015b3b5d2',
    '917f486e-e48a-4e11-bd68-a531702b20a8',
    -- Logistics CH
    '9a185d56-180a-4450-8aee-ea98ac897f49',
    '88800ec7-b4fb-471c-ac30-2b759743cf67',
    '6ae94cd1-78e5-42a2-870c-54f4ddbfb9cd',
    '19f57576-eb75-417f-986d-a33cc83b6502',
    '90d38d22-a05e-46cb-a4e2-deea6d3cc9f4',
    -- Logistics International
    '9b7a6db5-fd27-4d8c-98fe-7f0ecab7188c',
    '17615f16-9d2b-42e7-9e61-b1f0a407bb3b',
    '0571b080-ec96-41d7-9852-e49ac432ab83',
    'abaef6e9-0172-4acd-9204-ff5980e976c2'
);
");
        }
    }
}
