# Seeding the Demo Line via Postman

`RegisterStation`/`RegisterStationEquipment` already exist as GraphQL mutations (routed through
MAF per 1.2) and own the validation for these rows (duplicate-station/equipment checks,
`StationAlreadyExistsException`, etc.). Demo Station/StationEquipment rows go through them rather
than raw INSERTs, so seeding exercises the same path the app uses at runtime. `EquipmentTypes` is
a plain reference table with no owning service (same as `Roles`), so it's still seeded via
`002.SeedEquipmentTypesTable.sql` — run that (and the migration) before this.

## Setup

Same as [`postman-auth-testing.md`](./postman-auth-testing.md): log in first, set
`Authorization: Bearer <accessToken>` on every request below. Facility id for the demo line is
the stubbed `FAC-001` (see `ARCHITECTURE.md` — `FacilityId` stays a single stubbed value this
sprint).

## 1. Register the line's stations

`POST https://localhost:7162/graphql`

```graphql
mutation Register($facilityId: String!, $stationId: String!, $predecessorStationIds: [String!], $isLoadingDock: Boolean!) {
  registerStation(facilityId: $facilityId, stationId: $stationId, predecessorStationIds: $predecessorStationIds, isLoadingDock: $isLoadingDock) {
    facilityId
    stationId
    isLoadingDock
    predecessorStationIds
  }
}
```

Run once per station, `facilityId = "FAC-001"`, `stationId` in order, `predecessorStationIds: []` and `isLoadingDock: false` except where noted:

1. `ST-INTAKE` — `predecessorStationIds: []`
2. `ST-ASSEMBLY` — `predecessorStationIds: ["ST-INTAKE"]`
3. `ST-PACK` — `predecessorStationIds: ["ST-ASSEMBLY"]`
4. `ST-DOCK` — `predecessorStationIds: ["ST-PACK"]`, `isLoadingDock: true`

## 2. Register each station's equipment

```graphql
mutation RegisterEquipment(
  $facilityId: String!
  $stationId: String!
  $equipmentId: String!
  $equipmentTypeId: Int!
) {
  registerStationEquipment(
    facilityId: $facilityId
    stationId: $stationId
    equipmentId: $equipmentId
    equipmentTypeId: $equipmentTypeId
  ) {
    facilityId
    stationId
    equipmentId
  }
}
```

`equipmentTypeId` values come from the seeded `EquipmentTypes` table (`SELECT id, name FROM
dbo.EquipmentTypes` after running `002.SeedEquipmentTypesTable.sql`). One equipment record per
station is enough to exercise the workflow diagram/`GetStationWorkflow`:

| stationId      | equipmentId     | equipmentType |
|----------------|-----------------|---------------|
| `ST-INTAKE`    | `EQ-INTAKE-01`  | Conveyor      |
| `ST-ASSEMBLY`  | `EQ-ASSEMBLY-01`| Press         |
| `ST-PACK`      | `EQ-PACK-01`    | Scanner       |
| `ST-DOCK`      | `EQ-DOCK-01`    | Packer        |

## 3. Routing is wired by step 1

`RegisterStation` now owns `NextStationId`/`IsLoadingDock` directly via `predecessorStationIds`/
`isLoadingDock` - each predecessor listed in step 1 has its `NextStationId` pointed at the new
station as part of registering it, so no separate routing step is needed for a line registered
this way. `003.SeedStationRouting-Demo.sql` remains only as the historical record of how this
specific demo line's routing was stitched by hand before the mutation existed - a fresh line
doesn't need it.

## Verify

```graphql
query {
  getStationWorkflow(facilityId: "FAC-001", stationId: "ST-INTAKE") {
    stationId
    isLoadingDock
    equipment {
      equipmentId
      equipmentTypeName
      status
    }
  }
}
```

Should return all four stations in order, terminating at `ST-DOCK` with `isLoadingDock: true`.
