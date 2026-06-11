import { NextResponse, type NextRequest } from "next/server";
import { getFacetCounts } from "@/lib/api/job-ads";
import { facetDimensionSchema } from "@/lib/dto/job-ads";

/**
 * ADR 0067 Beslut 4 (Fas E2c) — facet-counts-proxy. Popover-klienten kan
 * inte anropa den `server-only` `getFacetCounts`-fetchern direkt
 * (session-cookie + BACKEND_URL är serverkontext). Speglar
 * `/api/jobb/suggest/route.ts`-mönstret: validera dimension billigt här
 * (allowlist — okänd dimension träffar aldrig backend), delegera till
 * server-fetchern, mappa `ApiResult` → HTTP-status.
 *
 * Degraderings-kontrakt (E2c-architect §5): alla fel-utfall → tomt objekt.
 * Klienten visar då inga counts men popovern förblir fullt användbar —
 * counts är en hint, aldrig en förutsättning (civic-utility).
 */
export async function GET(request: NextRequest) {
  const params = request.nextUrl.searchParams;

  const dimension = facetDimensionSchema.safeParse(params.get("dimension"));
  if (!dimension.success) {
    return NextResponse.json(
      { error: "Ogiltig dimension." },
      { status: 400 }
    );
  }

  const result = await getFacetCounts(dimension.data, {
    occupationGroup: params.getAll("occupationGroup"),
    municipality: params.getAll("municipality"),
    region: params.getAll("region"),
    q: params.get("q") ?? undefined,
  });

  switch (result.kind) {
    case "ok":
      return NextResponse.json(result.data);
    case "unauthorized":
      return NextResponse.json({}, { status: 401 });
    case "rateLimited":
      return NextResponse.json(
        {},
        {
          status: 429,
          headers: { "Retry-After": String(result.retryAfterSeconds) },
        }
      );
    // forbidden/notFound/error → tomt objekt, 200: counts degraderar tyst.
    default:
      return NextResponse.json({}, { status: 200 });
  }
}
