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
mutation Register($facilityId: String!, $stationId: String!) {
  registerStation(facilityId: $facilityId, stationId: $stationId) {
    facilityId
    stationId
  }
}
```

Run once per station, `facilityId = "FAC-001"`, `stationId` in order:

1. `ST-INTAKE`
2. `ST-ASSEMBLY`
3. `ST-PACK`
4. `ST-DOCK`

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

## 3. Wire the routing

`RegisterStation`/`RegisterStationEquipment` don't own `NextStationId`/`IsLoadingDock` (no
mutation exists for that field yet - it's a 1.3 data addition, not an operational write). Run
`003.SeedStationRouting-Demo.sql` after step 2 to chain `ST-INTAKE -> ST-ASSEMBLY -> ST-PACK ->
ST-DOCK` and mark `ST-DOCK` as the loading dock.

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
