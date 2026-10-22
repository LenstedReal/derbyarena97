package com.lenstedreal.derbyarena.game.systems

import org.junit.Assert.*
import org.junit.Test

class ExtractionRunSystemTest {
    @Test fun requiresThreeCellsBeforeExtraction() {
        val run = ExtractionRunSystem()
        assertEquals(0, run.securedCells)
        assertFalse(run.extractionUnlocked)
        // The extraction pad cannot complete the run before all cells are secured.
        val first = run.update(1f, 0f, -575f, true)
        assertFalse(first.extracted)
        assertEquals(0, run.securedCells)
    }

    @Test fun securingAllCellsUnlocksExtraction() {
        val run = ExtractionRunSystem()
        run.update(0.016f, -300f, 60f, true)
        run.update(0.016f, 285f, 95f, true)
        run.update(0.016f, -35f, -265f, true)
        assertEquals(3, run.securedCells)
        assertTrue(run.extractionUnlocked)
        val result = run.update(0.016f, 0f, -575f, true)
        assertTrue(result.extracted)
    }

    @Test fun runTimesOut() {
        val run = ExtractionRunSystem()
        val result = run.update(241f, 0f, 180f, true)
        assertTrue(result.timedOut)
    }
}
