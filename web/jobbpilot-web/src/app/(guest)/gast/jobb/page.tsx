import Link from "next/link";
import { ArrowRight, Search } from "lucide-react";

// F-Pre Punkt 5 — `/gast/jobb` konverterings-CTA (CTO-dom 2026-05-24 Beslut
// 3 Alt 2 + Klas-GO 2026-05-24 STOPP A).
//
// /jobb LIVE för gäst kräver `RequireAuthorization`-drop på `/api/v1/job-ads`
// + ADR 0005-amendment + säkerhets-/bot-trafik-mätning — deferrad till egen
// senare session. Tills dess: sidan finns inte i gäst-navet, men URL:n leder
// hit (graceful) med pedagogisk konverterings-CTA istället för 404.

export default function GuestJobbRoute() {
  return (
    <>
      <section className="jp-pagehero">
        <div className="jp-pagehero__inner">
          <div className="jp-pagehero__main">
            <div className="jp-pagehero__kicker">Sök jobb</div>
            <h1 className="jp-pagehero__title">Att söka kräver inloggning</h1>
            <p className="jp-pagehero__lede">
              Sökning bland Platsbanken-annonser är öppen för dig som har konto.
              Anmäl dig till väntelistan så hör vi av oss när du kan logga in.
            </p>
          </div>
        </div>
      </section>

      <div className="jp-container jp-page">
        <div className="jp-empty jp-empty--brand">
          <div className="jp-empty__kicker">Demo</div>
          <div className="jp-empty__title">Du är i demoläget</div>
          <p className="jp-empty__body">
            Du kan utforska översikten, exempel-ansökningar och CV-varianter
            här. När du har konto kan du också söka och spara annonser från
            Platsbanken direkt.
          </p>
          <div className="jp-empty__actions">
            <Link href="/vantelista" className="jp-btn jp-btn--primary">
              Anmäl till väntelistan <ArrowRight size={14} aria-hidden="true" />
            </Link>
            <Link href="/logga-in" className="jp-btn jp-btn--ghost">
              <Search size={14} aria-hidden="true" /> Jag har redan konto
            </Link>
          </div>
        </div>
      </div>
    </>
  );
}
