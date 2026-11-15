using SpaceTrace.Backend.Models;

namespace SpaceTrace.Backend.Data;

public interface IComponentCatalog
{
    IReadOnlyList<SpaceComponent> GetAll();
    SpaceComponent? GetById(string id);
}

public sealed class ComponentCatalog : IComponentCatalog
{
    internal const string IngenuityInvestigationUrl =
        "https://www.jpl.nasa.gov/news/nasa-performs-first-aircraft-accident-investigation-on-another-world/";

    private readonly IReadOnlyList<SpaceComponent> _components = CreateComponents();

    public IReadOnlyList<SpaceComponent> GetAll() => _components;

    public SpaceComponent? GetById(string id) => _components.FirstOrDefault(
        item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<SpaceComponent> CreateComponents()
    {
        const string nssdc = "https://nssdc.gsfc.nasa.gov/";
        const string nasaScience = "https://science.nasa.gov/mission/";

        return
        [
            new SpaceComponent(
                "apollo-11-descent-stage", "Apollo 11 Lunar Module Descent Stage", "Lunar module stage", "Moon",
                "Apollo 11", 1969,
                "The lower half of the Eagle lunar module that delivered Neil Armstrong and Buzz Aldrin to the Moon.",
                "It provided the propulsion and landing structure for the final descent to the lunar surface.",
                "The ascent stage lifted the astronauts back into lunar orbit. The descent stage remained at the landing site.",
                "Left on the lunar surface at Tranquility Base. Its present physical condition has not been directly inspected.",
                "Apollo 11 Eagle lunar module descent stage",
                [
                    new SourceReference("NSSDCA mission record – Apollo 11 descent stage", nssdc + "nmc/spacecraft/display.action?id=1969-059C", "NASA NSSDCA", true),
                    new SourceReference("NSSDCA – NASA Space Science Data Coordinated Archive", nssdc, "NASA NSSDCA", true)
                ]),

            new SpaceComponent(
                "apollo-lunar-roving-vehicle", "Apollo Lunar Roving Vehicle", "Crewed electric rover", "Moon",
                "Apollo 15, Apollo 16 and Apollo 17", 1971,
                "The battery-powered lunar vehicle that let Apollo astronauts travel farther from their landing sites.",
                "It carried astronauts, tools and samples across the lunar surface during the later Apollo missions.",
                "The rovers were left on the Moon when the crews departed in their lunar modules.",
                "Three Apollo lunar roving vehicles remain at their respective lunar mission sites.",
                "Apollo lunar roving vehicle moon buggy",
                [
                    new SourceReference("NSSDCA mission record – Lunar Roving Vehicle", nssdc + "nmc/spacecraft/display.action?id=1971-063C", "NASA NSSDCA", true),
                    new SourceReference("NSSDCA – NASA Space Science Data Coordinated Archive", nssdc, "NASA NSSDCA", true)
                ]),

            new SpaceComponent(
                "apollo-alsep", "Apollo Lunar Surface Experiments Package (ALSEP)", "Scientific instrument package", "Moon",
                "Apollo surface missions", 1969,
                "A collection of experiments deployed by astronauts to continue collecting measurements after they left the Moon.",
                "ALSEP instruments studied the lunar environment. Packages included experiments such as seismometers and other scientific instruments.",
                "The instruments were designed to operate on the lunar surface without the astronauts present. NASA ended the network's operations in 1977.",
                "The experiment packages remain at their deployment sites; individual instrument condition is not directly known.",
                "Apollo ALSEP lunar surface experiments package seismometer",
                [
                    new SourceReference("NSSDCA mission record – ALSEP", nssdc + "nmc/spacecraft/display.action?id=1969-099C", "NASA NSSDCA", true),
                    new SourceReference("NSSDCA – NASA Space Science Data Coordinated Archive", nssdc, "NASA NSSDCA", true)
                ]),

            new SpaceComponent(
                "surveyor-3", "Surveyor 3 Lunar Lander", "Robotic lander", "Moon",
                "Surveyor 3", 1967,
                "A robotic spacecraft that made a soft landing on the Moon before the Apollo 12 crew visited the site.",
                "Surveyor missions tested technologies and gathered information needed for later crewed lunar landings.",
                "Surveyor 3 completed its mission on the lunar surface; Apollo 12 astronauts later visited it and returned selected parts to Earth.",
                "The lander remains at its lunar landing site, with selected components returned to Earth by Apollo 12.",
                "Surveyor 3 lunar lander Apollo 12",
                [
                    new SourceReference("Surveyor 3 – NASA Science", nasaScience + "surveyor-3/", "NASA Science", true)
                ]),

            new SpaceComponent(
                "sojourner-rover", "Sojourner Mars Rover", "Microrover", "Mars",
                "Mars Pathfinder", 1997,
                "The first successful rover to operate on Mars, delivered to the surface by the Mars Pathfinder lander.",
                "It tested mobility and performed close-up investigations of Martian rocks and soil.",
                "Sojourner was a technology demonstration and science rover. Communication was lost after the Pathfinder lander stopped operating.",
                "The rover remains at the Mars Pathfinder landing site; its present condition has not been directly inspected.",
                "Mars Pathfinder Sojourner rover",
                [
                    new SourceReference("Mars Pathfinder – NASA Science", nasaScience + "mars-pathfinder/", "NASA Science", true)
                ]),

            new SpaceComponent(
                "spirit-opportunity", "Spirit and Opportunity Mars Rovers", "Twin robotic rovers", "Mars",
                "Mars Exploration Rover mission", 2004,
                "Twin rovers sent to Mars to investigate rocks, soil and the history of water on the planet.",
                "They searched for geological evidence that liquid water had affected the Martian environment.",
                "Both rovers eventually stopped communicating after operating far beyond their original planned mission duration.",
                "Both rovers remain on Mars. Spirit's final communication was in 2010; Opportunity's mission ended in 2019.",
                "Mars Exploration Rovers Spirit Opportunity",
                [
                    new SourceReference("Mars Exploration Rovers: Spirit and Opportunity – NASA Science", nasaScience + "mars-exploration-rovers-spirit-and-opportunity/", "NASA Science", true)
                ]),

            new SpaceComponent(
                "viking-1-lander", "Viking 1 Lander", "Robotic lander", "Mars",
                "Viking 1", 1976,
                "One of the first spacecraft to successfully operate on the Martian surface.",
                "It returned images and environmental measurements and carried experiments designed to investigate the Martian soil.",
                "Viking 1 operated on the surface until communications ended in 1982.",
                "The lander remains at its original landing site on Mars; no direct inspection has taken place since the mission.",
                "Viking 1 Mars lander",
                [
                    new SourceReference("Viking 1 – NASA Science", nasaScience + "viking-1/", "NASA Science", true)
                ]),

            new SpaceComponent(
                "phoenix-lander", "Phoenix Mars Lander", "Robotic lander", "Mars",
                "Phoenix", 2008,
                "A Mars lander that used a robotic arm to dig into the soil of the planet's northern plains.",
                "It investigated the soil and confirmed the presence of water ice beneath the surface.",
                "The mission ended after the lander could no longer maintain power through the Martian winter.",
                "The lander remains at its landing site. Its current physical condition is not directly observed.",
                "Phoenix Mars lander water ice",
                [
                    new SourceReference("Phoenix & InSight Landers – NASA Science", nasaScience + "insight/", "NASA Science", true,
                        "This link is the challenge-listed combined resource, which currently resolves to the InSight mission page."),
                    new SourceReference("Mars Phoenix – NASA Science", nasaScience + "mars-phoenix/", "NASA Science", false,
                        "Additional NASA mission reference for Phoenix-specific facts.")
                ]),

            new SpaceComponent(
                "insight-lander", "InSight Mars Lander", "Geophysical lander", "Mars",
                "Interior Exploration using Seismic Investigations, Geodesy and Heat Transport (InSight)", 2018,
                "A stationary Mars lander that studied the planet's internal structure.",
                "Its seismometer recorded marsquakes and helped scientists investigate the planet's interior.",
                "The mission ended after dust accumulation on its solar panels reduced available power and communications ceased.",
                "InSight remains at its landing site. NASA's final mission status is historical rather than a live health reading.",
                "NASA InSight Mars lander seismometer",
                [
                    new SourceReference("InSight Lander – NASA Science", nasaScience + "insight/", "NASA Science", true)
                ]),

            new SpaceComponent(
                "ingenuity-helicopter", "Ingenuity Mars Helicopter", "Robotic helicopter", "Mars",
                "Mars 2020 / Perseverance", 2021,
                "The small helicopter that demonstrated powered, controlled flight on another planet.",
                "It tested whether rotorcraft could fly in the thin Martian atmosphere and scout routes for future exploration.",
                "The aircraft completed its technology-demonstration mission after rotor damage ended further flight operations.",
                "Ingenuity is stationary on the Martian surface. Its flight mission ended in January 2024.",
                "Ingenuity Mars helicopter",
                [
                    new SourceReference("Ingenuity Mars Helicopter – NASA Science", nasaScience + "mars-2020-perseverance/ingenuity-mars-helicopter/", "NASA Science", true),
                    new SourceReference("Ingenuity’s final flight investigation – NASA JPL", IngenuityInvestigationUrl, "NASA JPL", false)
                ])
        ];
    }
}
