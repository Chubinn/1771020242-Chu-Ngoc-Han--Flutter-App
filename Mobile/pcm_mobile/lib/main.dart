import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'bloc/auth_bloc.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen_new.dart';
import 'services/api_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final apiService = ApiService();
  await apiService.init();
  runApp(MyApp(apiService: apiService));
}

class MyApp extends StatelessWidget {
  final ApiService apiService;

  const MyApp({super.key, required this.apiService});

  @override
  Widget build(BuildContext context) {
    return MultiRepositoryProvider(
      providers: [RepositoryProvider<ApiService>(create: (_) => apiService)],
      child: MultiBlocProvider(
        providers: [
          BlocProvider<AuthBloc>(
            create: (context) => AuthBloc(apiService)..add(GetCurrentUserEvent()),
          ),
        ],
        child: MaterialApp(
          title: 'PCM Mobile',
          theme: ThemeData(primarySwatch: Colors.teal, useMaterial3: true),
          home: BlocBuilder<AuthBloc, AuthState>(
            builder: (context, state) {
              if (state is AuthAuthenticatedState) {
                return HomeScreen();
              }
              if (state is AuthLoadingState) {
                return const Scaffold(
                  body: Center(child: CircularProgressIndicator()),
                );
              }
              return LoginScreen();
            },
          ),
          debugShowCheckedModeBanner: false,
        ),
      ),
    );
  }
}
