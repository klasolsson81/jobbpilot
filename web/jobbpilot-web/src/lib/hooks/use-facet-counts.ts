"use client";

import { useEffect, useRef, useState } from "react";
import {
  facetCountsSchema,
  FACET_COUNTS_DEBOUNCE_MS,
  type FacetCounts,
  type FacetDimension,
} from "@/lib/dto/job-ads";

/**
 * ADR 0067 Beslut 4 (Fas E2c) — debouncad klient-hämtning av per-option
 * facet-counts mot `/api/jobb/facet-counts` (proxy → backend
 * FacetCountsPolicy 30/10s). Self-contained debounce ≥300 ms +
 * AbortController (typeahead-prejudikatet — INTE TanStack Query, ADR 0042-
 * notatet; §4.3 reglerar mutations/pollar, ej en popover-read).
 *
 * Returnerar `null` tills första lyckade svaret (eller vid degradering) —
 * konsumenten visar då inga counts; popovern förblir användbar
 * (counts är en hint, aldrig en förutsättning). `enabled=false` (stängd
 * popover) avbryter in-flight och behåller `null` — ingen bakgrunds-poll.
 */
export interface FacetCountsFilterState {
  occupationGroup: ReadonlyArray<string>;
  municipality: ReadonlyArray<string>;
  region: ReadonlyArray<string>;
  q: string;
}

export function useFacetCounts(
  dimension: FacetDimension,
  filter: FacetCountsFilterState,
  enabled: boolean,
): FacetCounts | null {
  const [counts, setCounts] = useState<FacetCounts | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  // Stabil dependency-nyckel — listorna är nya referenser per render.
  const filterKey = JSON.stringify([
    filter.occupationGroup,
    filter.municipality,
    filter.region,
    filter.q,
  ]);

  useEffect(() => {
    if (!enabled) {
      abortRef.current?.abort();
      // Behåll senaste counts under stängning (ingen flimmer-nollning);
      // nästa öppning re-fetchar via enabled-flippen.
      return;
    }

    const timer = setTimeout(async () => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;

      const params = new URLSearchParams({ dimension });
      const [occupationGroup, municipality, region, q] = JSON.parse(
        filterKey,
      ) as [string[], string[], string[], string];
      for (const v of occupationGroup) params.append("occupationGroup", v);
      for (const v of municipality) params.append("municipality", v);
      for (const v of region) params.append("region", v);
      const trimmedQ = q.trim();
      if (trimmedQ.length >= 2) params.set("q", trimmedQ);

      try {
        const res = await fetch(`/api/jobb/facet-counts?${params}`, {
          signal: controller.signal,
        });
        if (!res.ok) {
          setCounts(null);
          return;
        }
        const parsed = facetCountsSchema.safeParse(await res.json());
        setCounts(parsed.success ? parsed.data : null);
      } catch {
        // Abort/nätverksfel → tyst degradering (behåll/nolla utan krasch).
        if (!controller.signal.aborted) setCounts(null);
      }
    }, FACET_COUNTS_DEBOUNCE_MS);

    return () => clearTimeout(timer);
  }, [dimension, filterKey, enabled]);

  return counts;
}
