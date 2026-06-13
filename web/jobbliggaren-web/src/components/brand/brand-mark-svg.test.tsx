import { describe, it, expect } from "vitest";
import { render } from "@testing-library/react";
import { BrandMarkSvg } from "./brand-mark-svg";

const fills = { primaryFill: "#15603F", accentFill: "#E8C77B", paperFill: "#FFFFFF" };

describe("BrandMarkSvg", () => {
  it("renderar sigill-geometri: 2 circles + 3 rects + 1 path", () => {
    const { container } = render(<BrandMarkSvg width={32} height={32} {...fills} />);
    expect(container.querySelectorAll("circle").length).toBe(2);
    expect(container.querySelectorAll("rect").length).toBe(3);
    expect(container.querySelectorAll("path").length).toBe(1);
  });

  it("disc-circle har cx=50 cy=50 r=45 + primaryFill (geometri-lock)", () => {
    const { container } = render(<BrandMarkSvg width={32} height={32} {...fills} />);
    const disc = container.querySelectorAll("circle")[0]!;
    expect(disc.getAttribute("cx")).toBe("50");
    expect(disc.getAttribute("cy")).toBe("50");
    expect(disc.getAttribute("r")).toBe("45");
    expect(disc.getAttribute("fill")).toBe("#15603F");
  });

  it("inre ring har r=37, fill=none, stroke=paperFill", () => {
    const { container } = render(<BrandMarkSvg width={32} height={32} {...fills} />);
    const ring = container.querySelectorAll("circle")[1]!;
    expect(ring.getAttribute("r")).toBe("37");
    expect(ring.getAttribute("fill")).toBe("none");
    expect(ring.getAttribute("stroke")).toBe("#FFFFFF");
  });

  it("mittraden använder accentFill, de andra två raderna paperFill", () => {
    const { container } = render(<BrandMarkSvg width={32} height={32} {...fills} />);
    const rects = container.querySelectorAll("rect");
    expect(rects[0]!.getAttribute("fill")).toBe("#FFFFFF");
    expect(rects[1]!.getAttribute("fill")).toBe("#E8C77B");
    expect(rects[2]!.getAttribute("fill")).toBe("#FFFFFF");
  });

  it("bock-path använder primaryFill som stroke", () => {
    const { container } = render(<BrandMarkSvg width={32} height={32} {...fills} />);
    const path = container.querySelector("path")!;
    expect(path.getAttribute("stroke")).toBe("#15603F");
    expect(path.getAttribute("fill")).toBe("none");
  });

  it("har konstant viewBox 0 0 100 100", () => {
    const { container } = render(<BrandMarkSvg width={180} height={180} {...fills} />);
    expect(container.querySelector("svg")!.getAttribute("viewBox")).toBe("0 0 100 100");
  });

  it("propagerar width/height", () => {
    const { container } = render(<BrandMarkSvg width={240} height={240} {...fills} />);
    const svg = container.querySelector("svg")!;
    expect(svg.getAttribute("width")).toBe("240");
    expect(svg.getAttribute("height")).toBe("240");
  });

  it("propagerar className + ariaLabel + ariaHidden", () => {
    const { container } = render(
      <BrandMarkSvg
        width={32}
        height={32}
        {...fills}
        className="jp-brand__mark"
        ariaLabel="Jobbliggaren"
        ariaHidden={false}
      />
    );
    const svg = container.querySelector("svg")!;
    expect(svg.getAttribute("class")).toBe("jp-brand__mark");
    expect(svg.getAttribute("aria-label")).toBe("Jobbliggaren");
    expect(svg.getAttribute("aria-hidden")).toBe("false");
  });
});
