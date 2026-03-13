# Reference Sheets

Curated data outputs used for balancing the "Not Just For Looks" mod thrusters and related block analytics. These are generated via PowerShell scripts in the `Scripts/` directory. Outputs here are treated as reference artifacts (not hand‑edited) and can be safely regenerated at any time.

## Files

- `thruster_mass_report.csv`
  - Source: `Scripts/thruster_mass_report.ps1`
  - Contains per-thruster component-derived mass, power draw, thrust, thrust-to-mass (N_per_kg) and efficiency (kN_per_MW). Includes Base, Mod (Upgraded/Advanced), and optional Prototech rows when flags provided.
- `thruster_comparison_report.csv`
  - Source: `Scripts/thruster_comparison_report.ps1`
  - Cross-tier comparison (Base vs Upgraded vs Advanced) with multiplier columns (`*_Force_x`, `*_Mass_x`, `*_N_per_kg_x`, `*_kN_per_MW_x`) and anomaly detection.
- `thruster_thrust_to__mass_ratios.csv`
  - Source: Initially aggregated; augmented by `append_recalculated_thrusts.ps1` and modified by `adjust_upgraded_thrusts.ps1`.
  - Serves as the authoritative mapping for enforcing policy: Upgraded = 2× Base N/kg, Advanced = 3× Base N/kg.
  - Columns `Recalc Thrust` and `Recalc Thrust To Mass` reflect the policy-driven recalculated values (mass aware) without overwriting the original Upgraded/Advanced fields until applied manually to `.sbc`.
- `reference_files_definitions.csv` / `reference_mods_definitions.csv`
  - Source: `Scripts/extract_base_definitions.ps1` & `Scripts/extract_mod_definitions.ps1` respectively.
  - Generic block definition extraction (SubtypeId, DisplayName, Volume, etc.) from base and mod `.sbc` files for broader auditing.
- `*_debug.txt`
  - Verbose or diagnostic captures of the data extraction passes (useful for tracing edge cases or parsing issues).
- `dryrun_output.txt`
  - Sample dry run output from the thrust adjustment script showing prospective changes prior to applying them in definition files.

## Script Overview

| Script | Purpose | Key Params |
| ------ | ------- | ---------- |
| `thruster_mass_report.ps1` | Aggregate thrust, mass, and efficiency per thruster (optionally include Base / Prototech). | `-IncludeBase`, `-IncludePrototech` |
| `thruster_comparison_report.ps1` | Build tier comparison rows + multipliers + anomaly flags. | Threshold params for anomalies |
| `append_recalculated_thrusts.ps1` | Append or recompute recalculated policy thrust + ratio columns. | `-UpgradedMultiplier`, `-AdvancedMultiplier` |
| `adjust_upgraded_thrusts.ps1` | Rewrite Upgraded/Advanced thrust columns in the CSV to exact policy values (mass-aware). | `-DryRun`, `-SkipExistingAccurate`, `-TolerancePct` |
| `extract_base_definitions.ps1` | Parse base game block `.sbc` definitions into a flat CSV. | `-OutFile` |
| `extract_mod_definitions.ps1` | Parse mod block `.sbc` definitions into a flat CSV. | `-OutFile` |

## Workflow (Thruster Balancing)
1. Run `thruster_mass_report.ps1 -IncludeBase -IncludePrototech` to gather baseline & mod masses/efficiencies.
2. Run `thruster_comparison_report.ps1` to review existing multipliers and flag anomalies.
3. Run `append_recalculated_thrusts.ps1` to (re)compute `Recalc Thrust` policy targets.
4. Optionally `adjust_upgraded_thrusts.ps1 -DryRun` to preview, then without `-DryRun` to update CSV Upgraded/Advanced thrust fields (if desired). We currently preserve original values in `.sbc` by commenting and inserting recalculated lines manually.
5. Apply updated `<ForceMagnitude>` values into `CubeBlocks_Upgraded.sbc` and `CubeBlocks_Advanced.sbc` (comment originals → insert new).
6. Re-run reports; validate N/kg ratios: Upgraded ≈ 2× Base; Advanced ≈ 3× Base; Prototech unchanged.

## Policy Reference
- Upgraded Tier: `TargetRatio = BaseRatio * 2.0`
- Advanced Tier: `TargetRatio = BaseRatio * 3.0`
- Thrust computed mass-aware: `TargetThrust = TargetRatio * TierMass`
- Original ForceMagnitude lines remain commented in `.sbc` for audit trails.

## Future Enhancements (Suggested)
- Auto validation script: diff in-file `<ForceMagnitude>` vs `Recalc Thrust` column and emit pass/fail list.
- Power normalization: Adjust `MaxPowerConsumption` so kN/MW efficiency tracks baseline scaling (currently inflated due to thrust increases without power increases).
- Rollback utility: restore commented original thrust lines if policy changes.
- Unit tests (Pester) around CSV recalculation for regression safety.

## Regeneration Safety
All CSVs here are reproducible. If corruption or stale data occurs, delete the affected file(s) and re-run the appropriate script(s). No manual edits should be necessary.

---
Generated/maintained as part of the balancing toolchain for APEX / Not Just For Looks.
