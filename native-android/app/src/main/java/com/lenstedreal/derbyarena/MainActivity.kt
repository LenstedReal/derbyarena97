package com.lenstedreal.derbyarena

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import com.lenstedreal.derbyarena.game.GameViewModel
import com.lenstedreal.derbyarena.game.GameViewModelFactory
import com.lenstedreal.derbyarena.game.data.DerbyDatabase
import com.lenstedreal.derbyarena.game.data.DerbyRepository
import com.lenstedreal.derbyarena.game.ui.DerbyArenaGame
import com.lenstedreal.derbyarena.ui.theme.DerbyArenaTheme

class MainActivity : ComponentActivity() {
  override fun onCreate(savedInstanceState: Bundle?) {
    super.onCreate(savedInstanceState)
    
    // Initialize Room Database, DAO, and Repository
    val database = DerbyDatabase.getDatabase(this)
    val repository = DerbyRepository(database.derbyDao())
    val factory = GameViewModelFactory(application, repository)

    enableEdgeToEdge()
    setContent {
      DerbyArenaTheme {
        Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
          val gameViewModel: GameViewModel = viewModel(factory = factory)
          DerbyArenaGame(
              viewModel = gameViewModel,
              modifier = Modifier.padding(innerPadding)
          )
        }
      }
    }
  }
}

