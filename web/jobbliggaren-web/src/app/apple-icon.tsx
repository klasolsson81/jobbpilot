import { ImageResponse } from "next/og";
import { BrandMarkSvg } from "@/components/brand/brand-mark-svg";

// Next.js 16 file convention: dynamiskt genererad apple-touch-icon för iOS home screen.
// Storlek per Apple HIG: 180×180. Renderas server-side via ImageResponse (satori).
// Geometri från BrandMarkSvg SSOT (sigillet, logo-översyn 2026-06-13): grön skiva
// (fyllt → robust i satori) på vit yta, vit ring/rader + guld mittrad.

export const size = { width: 180, height: 180 };
export const contentType = "image/png";
export const runtime = "edge";

export default function AppleIcon() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: "#FFFFFF",
        }}
      >
        <BrandMarkSvg
          width={140}
          height={140}
          primaryFill="#15603F"
          accentFill="#E8C77B"
          paperFill="#FFFFFF"
        />
      </div>
    ),
    { ...size }
  );
}
