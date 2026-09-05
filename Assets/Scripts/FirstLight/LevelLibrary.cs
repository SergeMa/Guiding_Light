using System.Collections.Generic;

namespace FirstLight
{
    /// <summary>
    /// The thirty-two levels, as ASCII maps. Every map here has been checked with an
    /// exhaustive solver (player position x mirror orientations x cleared eyes), so
    /// each one is guaranteed reachable-and-solvable from its starting state.
    ///
    /// Legend
    ///   #  wall            .  floor           _  chasm (light crosses, you cannot)
    ///   >  < ^ v           fixed beam source, emitting in that direction
    ///   0-3 mirror, digit = starting orientation (0 NE, 1 NW, 2 SW, 3 SE face)
    ///   P  prism (splits a beam into both perpendiculars)
    ///   Y  lantern (burns while lit, casting light four ways; out when the beam leaves)
    ///
    ///   Held - these work only while their plate has the beam on it, and stop the
    ///   instant it moves on:
    ///   p  plate -> b bridge, H held door, n nightbloom (open only while p is DARK)
    ///   q  plate -> c bridge, h held door, m nightbloom (open only while q is DARK)
    ///
    ///   Latched - lit once, and they stay that way:
    ///   R  receiver -> D door      r receiver -> d door
    ///   T  one-shot switch -> G gate
    ///
    ///   F  focus lens -> K door (held while TWO beams cross the lens at once)
    ///
    ///   Darkness:
    ///   C  cloud - deadly everywhere until its eye is out
    ///   g  gloom - deadly only where the beam is not falling, so the light is a walkway;
    ///      a gloom with no eye at all is simply permanent dark
    ///   e  static eye     o  roaming eye     w  warded eye (needs two beams at once)
    ///   @  you              X  exit (must be lit, and every eye must be out)
    /// </summary>
    public static class LevelLibrary
    {
        public static List<LevelDef> Build() => new()
        {
            new LevelDef("First Light", "Rotate one mirror to bend the beam onto the exit.", @"
#########
#>.....0#
#.......#
#......X#
#..@....#
#########"),

            new LevelDef("Two Turns", "Two mirrors - the walk between them is part of the puzzle.", @"
###########
#>.......0#
#.........#
#....@....#
#.........#
#X.......0#
###########"),

            new LevelDef("The Crossing", "Light a plate to raise a bridge across the chasm.", @"
#############
#>.........0#
#....._.....#
#....._.....#
#.....b.....#
#X.p.._....0#
#....._...@.#
#############"),

            new LevelDef("Held Breath", "The bridge is lit by the beam you are about to need elsewhere.", @"
#############
#...._.....X#
#...._......#
#>..._.....2#
#...._......#
#....b......#
#...._......#
#...._......#
#.p.._.....1#
#@..._......#
#############"),

            new LevelDef("Locked Light", "A receiver opens the doors - one for the beam, one for you.", @"
#############
#...#0.....<#
#...#.......#
#...#R......#
#...#.......#
#..XD2......#
#...#.......#
#...D.....@.#
#############"),

            new LevelDef("Held Open", "This door stays open only while the plate is lit.", @"
#####v#####
#X...3...p#
#....#....#
#....H....#
#....#....#
#....#....#
#....#...@#
###########"),

            new LevelDef("The Watcher", "Strike the red eye. The cloud lifts and the light passes through.", @"
#############
#>........0.#
#...........#
#..CCCCC....#
#.XCCeCC..0.#
#..CCCCC....#
#...........#
#@..........#
#############"),

            new LevelDef("Carry the Light", "One beam, three jobs on its way down.", @"
###############
#>...........0#
#@............#
#............p#
#.............#
#____b________#
#.............#
#.............#
#.............#
#..X..CeC....0#
###############"),

            new LevelDef("The Vault", "A one-shot switch opens the gate; an eye guards the rest.", @"
#############
#>.........0#
#...#....@.T#
#...#.......#
#...#.......#
#...#.......#
#...#.......#
#.X.G.CeC..0#
#############"),

            new LevelDef("Flash", "One flash is enough. The gate does not close again.", @"
###########
#........X#
#####G###_#
#>.......0#
#.........#
#........T#
#.........#
#.........#
#@........#
###########"),

            new LevelDef("The Lantern", "A lamp that burns only while you shine on it.", @"
#############
#>.........0#
#@..........#
#.....p.....#
#...........#
#.....Y....0#
#...........#
######H######
#...........#
#.....X.....#
#############"),

            new LevelDef("Crosslight", "No mirror can line up on that eye. The lamp can.", @"
#############
#>.........0#
#...........#
#X....Y....0#
#...........#
#....CCC....#
#....CeC....#
#....CCC....#
#@..........#
#############"),

            new LevelDef("The Gloom", "Only the cells the beam touches are safe. Walk the light.", @"
###############
#..ggggggggg..#
#..ggggggggg..#
#X.gggeggggg.0#
#..ggggggggg..#
#>.ggggggggg.0#
#..ggggggggg..#
#..ggggggggg..#
#..ggggggggg..#
#@.ggggggggg..#
###############"),

            new LevelDef("Lead the Light", "The corridor turns where you turn it. Round the corners on the landings.", @"
##############
#..gggggggggg#
#..gggggggggg#
#>.gggggggg0g#
#..ggggggg.gg#
#..gggggggggg#
#..gggggggggg#
#..gggggggggg#
#..ggggggg.gg#
#X.gggggggg2g#
#..gggggggggg#
#@.gggggggggg#
##############"),

            new LevelDef("Lantern Chain", "Each lamp is a crossroads you stand on. Light makes more floor.", @"
###############
#..ggggggggggg#
#v.ggggggggggg#
#..ggggggggggg#
#..ggggggggggg#
#..ggggggggggg#
#..ggYgggYgggX#
#..ggggggggggg#
#..ggggggggggg#
#..ggggggggggg#
#2.ggYgggggggg#
#.@ggggggggggg#
###############"),

            new LevelDef("Floor and Door", "The lamp under your feet is what holds the way ahead open.", @"
###############
#..ggggggggggg#
#v.ggggggggggg#
#..gg0gggpgggg#
#..ggggggggggg#
#..ggggggggggg#
#2.ggYggHggggX#
#..ggggggggggg#
#..ggggggggggg#
#..ggggggggggg#
#..ggggggggggg#
#.@ggggggggggg#
###############"),

            new LevelDef("Split", "A prism divides the beam: one half unlocks, one half arrives.", @"
#############
#.R...0.....#
#...........#
#...........#
#...........#
#>....P....@#
#...........#
#...........#
#...........#
#.XD..0.....#
#############"),

            new LevelDef("Both Plates", "Two bridges, two plates, one beam - so it has to be split.", @"
##############
#......._.._.#
#.....0._.p_.#
#......._.._.#
#......._.._.#
#>....P.b.._.#
#......._..c.#
#......._.._.#
#.....2._.q_X#
#@......_.._.#
##############"),

            new LevelDef("Twin Eyes", "Two eyes, one beam. Split it and take both.", @"
###############
#..CCC........#
#..CeC..0.....#
#..CCC........#
#.............#
#>......P....@#
#.............#
#..CCC........#
#X.CeC..0.....#
#..CCC........#
###############"),

            new LevelDef("Focus", "A lens holds its door only while two beams cross on it.", @"
#######v#######
#@............#
#.............#
#.............#
#.............#
#.0....P....0.#
#.............#
#.............#
##.#######K#.##
##0....F_X._0##
###############"),

            new LevelDef("Pinch", "The lamp gives you the second beam the lens is waiting for.", @"
############
#........#.#
#....0..0#.#
#........K.#
#........#.#
#>...Y..F_X#
#........#.#
#........#.#
#........#.#
#@.......#.#
############"),

            new LevelDef("Warded", "One beam is not enough. This eye needs two at once.", @"
######v######
#@..........#
#...........#
#..0..P....0#
#...........#
#...........#
#...........#
#.CCC.......#
#XCwC......0#
#.CCC.......#
#############"),

            new LevelDef("Split Corridor", "Two corridors at once: one to walk, one to carry the light.", @"
###############
#...ggggggggg.#
#...ggggggggg.#
#.0.ggggggggg.#
#...ggggggggg.#
#...ggggggggg.#
#>P.ggggggggg.#
#...ggggggggg.#
#...ggggggggg.#
#.2.gggg_gggX.#
#...ggggggggg.#
#@..ggggggggg.#
###############"),

            new LevelDef("The Roaming Dark", "This eye wanders. Flood the whole cloud and let it walk in.", @"
###############
#>...........0#
#.............#
#XCCCCCCCCC..1#
#.CCCCoCCCC...#
#.CCCCCCCCC...#
#.............#
#.............#
#.............#
#@............#
###############"),

            new LevelDef("Two Locks", "A door for the beam, a bridge for you, an eye between.", @"
###############
#>...........0#
#@...........R#
######D######_#
#.............#
#.............#
#._...........#
#Xb.CeC.p....0#
#._...........#
#._...........#
###############"),

            new LevelDef("Shutter", "Aiming here shuts the way through. Aim somewhere else.", @"
###########
#....#...q#
#....#....#
#....#....#
#>..._...1#
#....m....#
#....#....#
#X..._...0#
#....#...@#
###########"),

            new LevelDef("Nightbloom", "This path closes in the light. Turn the beam away to cross.", @"
###############
#..R_._.q....0#
#...#.#.......#
#...#.#.......#
#...#.D.......#
#>.._._......0#
#...#.#.......#
#...m.#.....@.#
#...#.#.......#
#.X._._......0#
###############"),

            new LevelDef("Hold Both", "Two held doors at once - only a split beam can manage it.", @"
###############
#>._._.......0#
#..#.#.p......#
#..#.#........#
#..#.H........#
#..#.#.P.....0#
#..#.#........#
#..#.#........#
#X.h._.Y......#
#..#.#........#
#..#.#........#
#..#.#.q....@.#
###############"),

            new LevelDef("Walk the Beam", "The beam is the only floor. Cross, charge, cross back, cross again.", @"
#################
#...gggggggg..#.#
#...gggggggg..#.#
#...gggggggg..#.#
#...gggggggg..#.#
#.0.gggggggg.0#.#
#...gggggggg..#.#
#>0.gggggggg.R#.#
#...gggggggg..#.#
#.0.gggggggg..DX#
#...gggggggg..#.#
#@..gggggggg..#.#
#################"),

            // ---- The finale is one tower, climbed in three. Each room begins in the
            // state the last one ended in: same mirrors at the angles you left them,
            // standing on the exit you just reached, with more of the shaft revealed.

            new LevelDef("The Tower", "The way up is the room you are standing in. Clear it, then climb.", @"
###############
###############
###############
###############
##.############
###############
###############
###############
###############
############X##
#@............#
#>..........0.#
#.............#
#...CeC.....2.#
###############"),

            new LevelDef("The Long Climb", "You start on the step you just reached. The tower opens above it.", @"
###############
###############
###############
###############
##.############
#...1.......0.#
#.............#
#.............#
#.1.Y........X#
############@##
#.............#
#>..........1.#
#.............#
#...........1.#
###############"),

            new LevelDef("Return to the Sky", "Everything you set is still set. Send the light out of the top.", @"
###############
#.0.........X.#
#.............#
#.............#
##.############
#...3.......2.#
#.............#
#.............#
#.1.Y........@#
############.##
#.............#
#>..........1.#
#.............#
#...........1.#
###############"),
        };
    }
}
