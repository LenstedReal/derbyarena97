package com.lenstedreal.derbyarena.game.systems

import kotlin.math.hypot

/**
 * BLACKOUT RUN: a mission-first extraction loop, not an elimination-only arena.
 * Secure three reactor cells at marked industrial sectors, then reach the north extraction pad.
 * Coordinates are in the same game-unit space as VehiclePhysicsSystem (GLB scale is 0.04).
 */
class ExtractionRunSystem {
    data class Cell(val x: Float, val y: Float, val label: String, var secured: Boolean = false)
    data class TickResult(val securedNow: String? = null, val extracted: Boolean = false, val timedOut: Boolean = false)

    private val cells = mutableListOf(
        Cell(-300f, 60f, "PUMP STATION"),
        Cell(285f, 95f, "TURBINE BAY"),
        Cell(-35f, -265f, "COOLANT VAULT")
    )
    val totalCells: Int get() = cells.size
    val securedCells: Int get() = cells.count { it.secured }
    val secondsRemaining: Float get() = remaining
    val extractionUnlocked: Boolean get() = securedCells == totalCells
    val objectiveName: String get() = if (extractionUnlocked) "NORTH EXTRACTION" else cells.firstOrNull { !it.secured }?.label ?: "NORTH EXTRACTION"
    val targetX: Float get() = if (extractionUnlocked) 0f else cells.firstOrNull { !it.secured }?.x ?: 0f
    val targetY: Float get() = if (extractionUnlocked) -575f else cells.firstOrNull { !it.secured }?.y ?: -575f
    val targetDistance: Float get() = hypot(targetX, targetY)
    private var remaining = 240f
    private var finished = false

    fun reset() {
        cells.forEach { it.secured = false }
        remaining = 240f
        finished = false
    }

    fun update(dt: Float, x: Float, y: Float, playerAlive: Boolean): TickResult {
        if (finished) return TickResult()
        remaining = (remaining - dt.coerceAtLeast(0f)).coerceAtLeast(0f)
        if (!playerAlive || remaining <= 0f) {
            finished = true
            return TickResult(timedOut = true)
        }
        if (!extractionUnlocked) {
            val cell = cells.firstOrNull { !it.secured && hypot(x - it.x, y - it.y) <= 62f }
            if (cell != null) {
                cell.secured = true
                return TickResult(securedNow = cell.label)
            }
        } else if (hypot(x, y + 575f) <= 82f) {
            finished = true
            return TickResult(extracted = true)
        }
        return TickResult()
    }
}
