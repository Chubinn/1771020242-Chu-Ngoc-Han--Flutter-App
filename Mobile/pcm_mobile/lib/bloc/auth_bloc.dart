import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:pcm_mobile/models/index.dart';
import 'package:pcm_mobile/services/api_service.dart';

// Events
abstract class AuthEvent {}

class LoginEvent extends AuthEvent {
  final String email;
  final String password;

  LoginEvent(this.email, this.password);
}

class RegisterEvent extends AuthEvent {
  final String email;
  final String password;
  final String fullName;

  RegisterEvent(this.email, this.password, this.fullName);
}

class LogoutEvent extends AuthEvent {}

class GetCurrentUserEvent extends AuthEvent {}

// States
abstract class AuthState {}

class AuthInitialState extends AuthState {}

class AuthLoadingState extends AuthState {}

class AuthAuthenticatedState extends AuthState {
  final User user;
  final String token;

  AuthAuthenticatedState(this.user, this.token);
}

class AuthUnauthenticatedState extends AuthState {}

class AuthErrorState extends AuthState {
  final String message;

  AuthErrorState(this.message);
}

// BLoC
class AuthBloc extends Bloc<AuthEvent, AuthState> {
  final ApiService apiService;

  AuthBloc(this.apiService) : super(AuthInitialState()) {
    on<LoginEvent>((event, emit) async {
      emit(AuthLoadingState());
      try {
        final response = await apiService.login(
          email: event.email,
          password: event.password,
        );

        final token = response['token'] ?? '';
        await apiService.setToken(token.toString());

        final user = User.fromJson(response['user'] ?? {});
        emit(AuthAuthenticatedState(user, token));
      } catch (e) {
        emit(AuthErrorState(e.toString()));
      }
    });

    on<RegisterEvent>((event, emit) async {
      emit(AuthLoadingState());
      try {
        await apiService.register(
          email: event.email,
          password: event.password,
          fullName: event.fullName,
        );
        emit(AuthUnauthenticatedState());
      } catch (e) {
        emit(AuthErrorState(e.toString()));
      }
    });

    on<LogoutEvent>((event, emit) async {
      await apiService.clearToken();
      emit(AuthUnauthenticatedState());
    });

    on<GetCurrentUserEvent>((event, emit) async {
      try {
        emit(AuthLoadingState());
        final token = await apiService.getToken();
        if (token == null || token.isEmpty) {
          emit(AuthUnauthenticatedState());
          return;
        }
        await apiService.setToken(token, persist: false);
        final user = await apiService.getCurrentUser();
        emit(AuthAuthenticatedState(user, token));
      } catch (e) {
        emit(AuthUnauthenticatedState());
      }
    });
  }
}
